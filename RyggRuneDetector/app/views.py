"""
Definition of views.
"""
from xml.dom.minidom import Notation
from authlib.integrations.django_client import OAuth
from django.contrib.auth.decorators import login_required
from django.shortcuts import redirect
from django.contrib.auth import authenticate, login as oauthlogin, logout as oauthlogout
from django.contrib.auth.models import User
from datetime import datetime
from django.shortcuts import render
from django.http import HttpRequest
import base64
import io
import json
from django.http import JsonResponse
from .forms import ImageUploadForm
from PIL import Image, ImageFilter
from rune_ocr.inference import RuneDetection
import io
from django.views.decorators.csrf import csrf_exempt
from pathlib import Path
from django.urls import reverse
from requests.models import PreparedRequest
import requests
import jwt
from django.http import HttpResponseForbidden
from django.conf import settings
from decouple import config
from azure.storage.blob import BlobServiceClient, BlobClient
import uuid

# image_processing/views.py

oauth = OAuth()
oauth.register(name="b2c")


def login(request):
    redirect_uri = request.build_absolute_uri(reverse('oauth2'))
    return oauth.b2c.authorize_redirect(request, redirect_uri)

def oauth2(request):
    token = oauth.b2c.authorize_access_token(request)
    user_info = token.get('userinfo')

    login_user(user_info.get('emails')[0], request)
    return redirect('/')

def logout(request):
    oauthlogout(request)

    metadata = oauth.b2c.load_server_metadata()
    end_session_endpoint = metadata.get('end_session_endpoint')
    redirect_url = '/'

    if end_session_endpoint:
        params = {'post_logout_redirect_uri': request.build_absolute_uri('/')}
        req = PreparedRequest()
        req.prepare_url(end_session_endpoint, params)
        redirect_url = req.url

    return redirect(redirect_url)

def upload_blob(container_name, blob_name, data):
    blob_service_client = BlobServiceClient.from_connection_string(
        settings.AZURE_STORAGE_CONNECTION_STRING
    )
    blob_client = blob_service_client.get_blob_client(container=container_name, blob=blob_name)
    blob_client.upload_blob(data)

@login_required
def image_upload(request):
    if request.method == 'POST':
        form = ImageUploadForm(request.POST, request.FILES)
        if form.is_valid():
            image = form.cleaned_data['image']
            fileNameBase =  datetime.utcnow().strftime("%Y-%m-%d-%H:%M:%S")  + request.user.username + str(uuid.uuid4())
            imgincomming = Image.open(image)
            detector = RuneDetection(str(Path(__file__).parent/'rune_ocr_v2__yolov7_512_HQ2.onnx'))
            annotations, img_annotated  = detector.run(image = imgincomming, return_image=True, score_thresh=0.1)
            upload_blob('rune-images', fileNameBase, json.dumps({ 
                                                 "im_annotated" : serialize_image_to_json(img_annotated),
                                                 "im_incomming": serialize_image_to_json(imgincomming),
                                                 "annotations": annotations
                                                 }))
            return JsonResponse({'success': True, 
                                 'annotations' : annotations,
                                 'annotatedImage': serialize_image_to_json(img_annotated)})
        else:
            return JsonResponse({'success': False, 'error': 'Invalid form data.'})
    else:
        form = ImageUploadForm()
    return render(request, 'app/image_processing/upload.html', {'form': form})

def login_user(email, request):
    try:
        user = User.objects.get(email=email)
    except User.DoesNotExist:
        # If the user doesn't exist, create a new user
        user = User.objects.create_user(username=email, email=email)

    # Log in the user using Django's authentication system
    oauthlogin(request, user)

    # Set user in session
    request.session['user'] = {
        'username': email,  # Adjust this based on the user_info
        'is_authenticated': True,
    }

@csrf_exempt
def post_image(request):
    if request.method == 'POST':
        auth_header = request.headers.get('Authorization')
        if auth_header and auth_header.startswith('Bearer '):
            token = auth_header.split(' ')[1]
            try:
                jwks_response = requests.get(config("B2C_JWKS"))
                jwks_data = jwks_response.json()
                header = jwt.get_unverified_header(token)
                kid = header.get("kid")

                public_key = None
                for key in jwks_data["keys"]:
                    if key["kid"] == kid:
                        public_key = jwt.algorithms.RSAAlgorithm.from_jwk(key)
                
                decoded_payload = jwt.decode(token, public_key, algorithms=["RS256"], audience=config("B2C_CLIENT_ID"))
                # Here, you can customize token validation and user authorization logic
                # For example, check if the user is valid and has necessary permissions
                login_user(decoded_payload["emails"][0], request)
            except jwt.ExpiredSignatureError:
                return HttpResponseForbidden('Token has expired.')
            except jwt.DecodeError:
                return HttpResponseForbidden('Invalid token.')
        if not request.user.is_authenticated:
            return HttpResponseForbidden('Invalid token.')
        byte_data = request.body
        detector = RuneDetection(str(Path(__file__).parent/'rune_ocr_v2__yolov7_512_HQ2.onnx'))
        image = Image.open(io.BytesIO(byte_data));
        fileNameBase =  datetime.utcnow().strftime("%Y-%m-%d-%H:%M:%S")  + request.user.username + str(uuid.uuid4())
        
        annotations, img_annotated = detector.run(image=image,return_image= True,score_thresh=0.1)
        upload_blob('rune-images', fileNameBase, json.dumps({ 
                                                 "im_annotated" : serialize_image_to_json(img_annotated),
                                                 "im_incomming": serialize_image_to_json(image),
                                                 "annotations": annotations
                                                 }))
        return JsonResponse({'success': True, 
                             'annotations' : annotations, 
                             'annotatedImage': serialize_image_to_json(img_annotated)})
    else:
        return JsonResponse({'success': False, 'error': 'Invalid form data.'})

def serialize_image_to_json(image):
    # Convert the image to a byte stream
    image_byte_array = io.BytesIO()
    
    image.save(image_byte_array, format='PNG')  # You can choose the format as per your requirement
    # Encode the byte stream as base64
    image_base64 = base64.b64encode(image_byte_array.getvalue()).decode('utf-8')
    return image_base64;


def home(request):
    """Renders the home page."""
    assert isinstance(request, HttpRequest)
    return render(
        request,
        'app/index.html',
        {
            'title':'Home Page',
            'year':datetime.now().year,
        }
    )

def contact(request):
    """Renders the contact page."""
    assert isinstance(request, HttpRequest)
    return render(
        request,
        'app/contact.html',
        {
            'title':'Contact',
            'message':'Your contact page.',
            'year':datetime.now().year,
        }
    )

def about(request):
    """Renders the about page."""
    assert isinstance(request, HttpRequest)
    return render(
        request,
        'app/about.html',
        {
            'title':'About',
            'message':'Your application description page.',
            'year':datetime.now().year,
        }
    )
