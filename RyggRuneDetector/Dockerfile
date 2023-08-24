# Use the official Python image as the base image
FROM python:3.9

# Set environment variables to improve Python behavior within the container
ENV PYTHONDONTWRITEBYTECODE 1
ENV PYTHONUNBUFFERED 1

# Set the working directory in the container
WORKDIR /app

# Copy the requirements file and install dependencies
COPY requirements.txt /app/
COPY packages/rune_ocr-0.0.2.tar.gz /app/packages/
RUN apt-get update
RUN apt-get install libgl1-mesa-glx -y
RUN pip install -r requirements.txt
RUN pip install packages/rune_ocr-0.0.3.tar.gz



# Copy the Django project code into the container
COPY . /app/

# Collect static files (if applicable, adjust this according to your project's setup)
RUN python manage.py collectstatic --noinput

EXPOSE 80

# Start the Django development server (use the same command as you would use locally)
CMD gunicorn -w 10 -b 0:80 -t 180 RyggRuneDetector.wsgi
