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
            
            detector = RuneDetection(str(Path(__file__).parent/'model_RuneOCR_20230722.onnx'))
            im_annotated, annotations = detector.run(Image.open(image))
            
            return JsonResponse({'success': True, 'annotations' : annotations})
        else:
            return JsonResponse({'success': False, 'error': 'Invalid form data.'})
    else:
        form = ImageUploadForm()
    return render(request, 'app/image_processing/upload.html', {'form': form})

@csrf_exempt
def post_image(request):
    if request.method == 'POST':
       
        byte_data = request.body
        detector = RuneDetection(str(Path(__file__).parent/'model_RuneOCR_20230722.onnx'))
        im_annotated, annotations = detector.run(Image.open(io.BytesIO(byte_data)))
        return JsonResponse({'success': True, 'annotations' : annotations})
    else:
        return JsonResponse({'success': False, 'error': 'Invalid form data.'})

def serialize_image_to_json(image):
    # Convert the image to a byte stream
    image_byte_array = io.BytesIO()
    
    image.save(image_byte_array, format='PNG')  # You can choose the format as per your requirement

    # Encode the byte stream as base64
    image_base64 = base64.b64encode(image_byte_array.getvalue()).decode('utf-8')

    # Create a dictionary representing the image data
    image_data = {
        'format': image.format,
        'mode': image.mode,
        'size': image.size,
        'data': image_base64,
    }

    # Convert the dictionary to JSON
    json_data = json.dumps(image_data)

    return json_data

def process_image(image):
    # Open the uploaded image using Pillow
    img = Image.open(image)

    # Apply a filter (e.g., GaussianBlur)
    processed_img = img.filter(ImageFilter.GaussianBlur(radius=2))

    # Save the processed image in a temporary location
    processed_img_path = 'C:\\users\\lind\onedrive\\documents\\' + image.name
    processed_img.save(processed_img_path)

    return processed_img

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
