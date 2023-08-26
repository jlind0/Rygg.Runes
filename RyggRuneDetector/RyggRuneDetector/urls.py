"""
Definition of urls for RyggRunes.
"""

from datetime import datetime
from django.urls import path
from django.contrib import admin
from django.contrib.auth.views import LoginView, LogoutView
from app import forms, views


urlpatterns = [
    path('', views.home, name='home'),
    path('contact/', views.contact, name='contact'),
    path('about/', views.about, name='about'),
    path('admin/', admin.site.urls),
    path('upload/', views.image_upload, name='image_upload'),
    path('process_image/', views.post_image, name='post_image'),
    path('login/', views.login, name='login'),
    path('logout/', views.logout, name='logout'),
    path('oauth2/', views.oauth2, name='oauth2')
]
