# ERM Hospital System

ERM Hospital System la he thong quan ly benh vien tu nhan gom website cong khai, cong benh nhan va cac man hinh noi bo cho Admin, Doctor, Cashier.

## Chuc nang chinh

- Website cong khai: trang chu, dich vu, chuyen khoa, bac si, tin tuc, dat lich.
- Cong benh nhan: lich hen, ho so kham, don thuoc, hoa don, thanh toan QR/mock.
- Noi bo benh vien: dashboard, lich hen, benh nhan, ho so kham, chi dinh can lam sang, ke don, hoa don, thong bao, nhan su.
- Xac thuc: JWT, refresh token, RBAC, MFA baseline, lockout, reset password.
- AI chat sang loc trieu chung: chay local/private bang Ollama, co RAG keyword noi bo va fallback khi Ollama chua san sang.

## Cong nghe

- Backend: ASP.NET Core / .NET 10, Entity Framework Core, SQL Server.
- Frontend: Next.js 16, React 19, TypeScript, Tailwind CSS.
- Ha tang tuy chon: Redis, RabbitMQ, Docker Compose.
- AI local tuy chon: Ollama.

## Yeu cau moi truong

Can cai dat:

- Git
- .NET SDK 10
- Node.js 22+ va npm
- SQL Server local hoac SQL Server Express
- `sqlcmd` trong PATH
- Ollama neu muon dung AI local that
- Docker Desktop neu muon chay Redis/RabbitMQ hoac full compose

Mac dinh backend dung SQL Server:

```text
Server=localhost\MSSQLSERVER01
Database=ERMSystemHospitalDb
Windows Authentication
```

Neu SQL Server cua ban khac, sua connection string trong:

```text
BackE/ERMSystem.API/appsettings.json
```

hoac dung bien moi truong:

```powershell
$env:ConnectionStrings__HospitalConnection="Server=.;Database=ERMSystemHospitalDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;Encrypt=False"
```

## Clone project

```powershell
git clone https://github.com/vietha37/ERMSystem_CleanAchitecture.git
cd ERMSystem_CleanAchitecture
```

Neu ban dang dung remote cu, GitHub co the thong bao repo da chuyen sang URL moi o tren.

## Cai dat database

Chay PowerShell tai thu muc goc project:

```powershell
.\Infrastructure\database\bootstrap_full_test_dataset.ps1 -ServerInstance "localhost\MSSQLSERVER01"
```

Script nay se:

- Tao lai database `ERMSystemHospitalDb`.
- Tao schema benh vien.
- Seed danh muc, bac si, benh nhan, lich hen, ho so kham, don thuoc, hoa don va tai khoan test.
- Chuan hoa mot so du lieu tieng Viet.

Neu chi muon seed nhe hon:

```powershell
.\Infrastructure\database\bootstrap_erm_private_hospital.ps1 -ServerInstance "localhost\MSSQLSERVER01"
```

## Chay backend

Restore va build:

```powershell
dotnet restore BackE\ERMSystem.sln
dotnet build BackE\ERMSystem.sln
```

Chay API:

```powershell
dotnet run --project BackE\ERMSystem.API\ERMSystem.API.csproj --urls http://localhost:5219
```

API mac dinh:

```text
http://localhost:5219
```

Kiem tra nhanh:

```powershell
Invoke-RestMethod http://localhost:5219/health/live
```

OpenAPI/Scalar se hien thi khi backend chay o moi truong Development.

## Chay frontend

Mo terminal khac:

```powershell
cd FontE
npm install
npm run dev
```

Frontend mac dinh:

```text
http://localhost:3000
```

Frontend goi backend qua:

```text
NEXT_PUBLIC_API_URL=http://localhost:5219/api
```

Neu can set thu cong:

```powershell
$env:NEXT_PUBLIC_API_URL="http://localhost:5219/api"
npm run dev
```

## Chay AI chat local

Tinh nang AI chat co fallback noi bo, nen website van tra loi co ban neu chua cai Ollama.

De dung model AI local that, cai Ollama va chay:

```powershell
ollama pull llama3.1:8b
ollama run llama3.1:8b
```

Ollama mac dinh chay tai:

```text
http://localhost:11434
```

Config AI nam trong:

```text
BackE/ERMSystem.API/appsettings.Development.json
```

Mac dinh:

```json
{
  "AiSymptomChat": {
    "Enabled": true,
    "Provider": "Ollama",
    "BaseUrl": "http://localhost:11434",
    "GeneratePath": "/api/generate",
    "Model": "llama3.1:8b",
    "TimeoutSeconds": 60,
    "MaxInputCharacters": 1200
  }
}
```

Knowledge base RAG noi bo:

```text
BackE/ERMSystem.API/App_Data/ai/symptom-knowledge.json
```

Test endpoint AI:

```powershell
$body = @{ message = "sot ho dau hong 2 ngay" } | ConvertTo-Json
Invoke-RestMethod -Uri "http://localhost:5219/api/ai-symptom-chat" -Method Post -ContentType "application/json" -Body $body
```

## Tai khoan test

Mat khau mac dinh:

```text
123456
```

Nhom tai khoan seed thuong co:

```text
admin00..19
doctor00..19
cashier00..19
patient00..19
```

Mot so nhom cashier phu:

```text
cashierrec00..19
cashierops00..19
cashierpha00..19
cashierlab00..19
```

## Chay Redis va RabbitMQ tuy chon

Backend co fallback in-memory khi Redis/RabbitMQ tat. Neu muon chay dich vu phu:

```powershell
docker compose -f Infrastructure\docker-compose.dev.yml up -d
```

RabbitMQ management:

```text
http://localhost:15672
guest / guest
```

## Chay bang Docker Compose

Tai thu muc goc:

```powershell
docker compose up --build
```

Ports:

```text
Frontend: http://localhost:3000
Backend:  http://localhost:5219
Redis:    localhost:6379
RabbitMQ: localhost:5672, management http://localhost:15672
```

Luu y: compose mac dinh van can SQL Server truy cap duoc tu container qua `host.docker.internal`, hoac can set:

```powershell
$env:ERM_HOSPITAL_DB_CONNECTION_STRING="Server=host.docker.internal;Database=ERMSystemHospitalDb;User Id=...;Password=...;TrustServerCertificate=True;Encrypt=False"
```

## Lenh kiem tra truoc khi commit

Backend:

```powershell
dotnet build BackE\ERMSystem.sln
```

Frontend:

```powershell
cd FontE
npm run lint
npm run build
```

## Loi thuong gap

### Backend build bi khoa DLL

Neu gap loi dang:

```text
The process cannot access the file ... because it is being used by another process
```

Hay tat process API dang chay:

```powershell
Get-Process ERMSystem.API,dotnet -ErrorAction SilentlyContinue
Stop-Process -Name ERMSystem.API -ErrorAction SilentlyContinue
```

Sau do build lai.

### Frontend build bi EPERM trong `.next/cache`

Tat dev server dang chay, xoa cache neu can:

```powershell
Remove-Item -Recurse -Force FontE\.next
cd FontE
npm run build
```

### AI chat bao chua san sang

Kiem tra Ollama:

```powershell
ollama --version
ollama list
ollama run llama3.1:8b
```

Neu chua cai Ollama, chatbot van dung fallback tu knowledge base noi bo.

## Ghi chu bao mat

File `appsettings.json` hien co gia tri phuc vu demo/local. Khi deploy that, nen dua cac gia tri sau ra environment variables hoac secret manager:

- JWT key
- Connection strings
- Payment gateway secret
- Webhook secret
- Redis/RabbitMQ credential

Khong commit credential that len repository.
