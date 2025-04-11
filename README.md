# Bingo Prediction API

Ứng dụng API dự đoán kết quả Bingo sử dụng .NET 8 và Docker.

## Yêu cầu

- .NET 8 SDK
- Docker và Docker Compose
- Git

## Cài đặt

1. Clone repository:
```bash
git clone https://github.com/yourusername/bingo-prediction-api.git
cd bingo-prediction-api
```

2. Build và chạy ứng dụng bằng Docker Compose:

Môi trường development:
```bash
docker-compose -f docker-compose.yml -f docker-compose.override.yml up -d
```

Môi trường production:
```bash
docker-compose -f docker-compose.yml -f docker-compose.prod.yml up -d
```

## API Endpoints

- `GET /api/bingo/latest-results`: Lấy 5 kết quả Bingo gần nhất từ API.
- `GET /api/bingo/latest-summaries`: Lấy 5 bản tóm tắt dự đoán gần nhất.
- `GET /api/bingo/check-prediction-accuracy`: Kiểm tra độ chính xác của dự đoán.

## Cấu trúc dự án

- `Bingo.ApiService`: API service chính
- `Bingo.PredictionService`: Service dự đoán kết quả Bingo
- `Bingo.Common`: Thư viện chung chứa các model và utility

## Phát triển

1. Chạy ứng dụng trong môi trường development:
```bash
dotnet run --project Bingo.ApiService
```

2. Chạy tests:
```bash
dotnet test
```

## License

MIT
