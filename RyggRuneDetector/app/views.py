"""
Definition of views.
"""

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
# image_processing/views.py

def image_upload(request):
    if request.method == 'POST':
        form = ImageUploadForm(request.POST, request.FILES)
        if form.is_valid():
            image = form.cleaned_data['image']
            
            detector = RuneDetection(str(Path(__file__).parent/'rune_ocr_v2__yolov7_512_HQ2.onnx'))
            annotations, img_annotated  = detector.run(image = Image.open(image), return_image=True, score_thresh=0.1)
            
            return JsonResponse({'success': True, 
                                 'annotations' : annotations,
                                 'annotatedImage': serialize_image_to_json(img_annotated)})
        else:
            return JsonResponse({'success': False, 'error': 'Invalid form data.'})
    else:
        form = ImageUploadForm()
    return render(request, 'app/image_processing/upload.html', {'form': form})

@csrf_exempt
def post_image(request):
    if request.method == 'POST':
       
        byte_data = request.body
        detector = RuneDetection(str(Path(__file__).parent/'rune_ocr_v2__yolov7_512_HQ2.onnx'))
        image = Image.open(io.BytesIO(byte_data));
        annotations, img_annotated = detector.run(image=image,return_image= True,score_thresh=0.1)
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
