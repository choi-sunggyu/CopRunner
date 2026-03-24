import urllib.request

API_KEY = "AIzaSyAzmdcN_l9g36RgH4aVFzt9YWk_ECECxj8"

url = (
    f"https://maps.googleapis.com/maps/api/staticmap"
    f"?center=37.5665,126.9780"
    f"&zoom=16"
    f"&size=600x400"
    f"&maptype=roadmap"
    f"&key={API_KEY}"
)

print(f"요청 URL: {url}")

try:
    with urllib.request.urlopen(url) as response:
        data = response.read()
        with open("test.jpg", "wb") as f:
            f.write(data)
        print(f"✅ 성공! 파일 크기: {len(data)} bytes")
except urllib.error.HTTPError as e:
    error = e.read().decode("utf-8")
    print(f"❌ 실패: {e.code}")
    print(f"응답: {error}")