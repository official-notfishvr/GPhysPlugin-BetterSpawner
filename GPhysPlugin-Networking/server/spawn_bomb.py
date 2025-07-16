import requests

url = "https://flask-hello-world-blue-phi.vercel.app/spawn_object"
data = {
    "object_type": "Bomb",
    "position": {"x": -66.7356, "y": 5.1706, "z": -69.635},
    "rotation": {"x": 0, "y": 0, "z": 0, "w": 1},
    "owner_id": "test_user"
}

response = requests.post(url, json=data)
print("Status:", response.status_code)
print("Response:", response.json()) 