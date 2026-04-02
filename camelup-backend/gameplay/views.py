import subprocess
from django.http import JsonResponse

def run_simulation(request):
    result = subprocess.run(
        ["dotnet", "run"],
        capture_output=True,
        text=True,
        cwd="path/to/CamelUpSimulator"
    )

    return JsonResponse({
        "output": result.stdout
    })

# Create your views here.
