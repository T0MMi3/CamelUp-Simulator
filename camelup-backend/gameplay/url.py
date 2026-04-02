from django.urls import path
from .views import run_simulation

urlpatterns = [
    path('simulate/', run_simulation),
]