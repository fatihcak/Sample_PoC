# AI Model Evaluation Data Center — PoC

Yapay zeka modellerinin gerçek zamanlı performansını izleyen bir değerlendirme merkezi denemesi.

## Mimari

```
MockProducers → RabbitMQ → WebApi (Consumer) → PostgreSQL
                                                     ↑
                                              React Dashboard
```

## Teknolojiler

| Katman | Teknoloji |
|---|---|
| Backend | .NET 10 / C# |
| Veritabanı | PostgreSQL (Docker) |
| Mesaj Kuyruğu | RabbitMQ (Docker) |
| ORM | Entity Framework Core |
| Frontend | React + Vite + TypeScript |
| Grafik | Chart.js |

## Çalıştırma

### Ön Koşullar
- Docker Desktop
- .NET 10 SDK
- Node.js

### 1. Altyapıyı Başlat (PostgreSQL + RabbitMQ)
```powershell
docker-compose up -d
```

### 2. Backend API (Terminal 1)
```powershell
dotnet run --project AiModelEvalCenter.WebApi
```
`Now listening on: http://localhost:5181` yazısını gördüğünde hazır.

### 3. Simülatörü Başlat (Terminal 2)
```powershell
dotnet run --project AiModelEvalCenter.MockProducers
```
`Published Batch Frame 1, 2, 3...` çıktısını görmelisin.

### 4. Dashboard (Terminal 3)
```powershell
cd AiModelEvalCenter.WebUI
npm install
npm run dev
```
Tarayıcıda `http://localhost:5173` adresini aç.

### 5. RabbitMQ Yönetim Paneli
`http://localhost:15672` — kullanıcı: `guest` / şifre: `guest`

---

## Dayanıklılık (Resilience) Testi

Sistem çalışırken veritabanı bağlantısının kesilmesi durumunda mesajların kaybolmadığını test edebilirsin:

1. Docker Desktop'tan `baykar_postgres` konteynerini durdur
2. RabbitMQ panelinde `q.telemetry.ingest` kuyruğunu izle → mesajlar **Ready** sütununda birikir
3. `baykar_postgres`'i tekrar başlat
4. Biriken mesajlar otomatik işlenir, dashboard rakamları yükselir

> **Not:** WebApi başlarken PostgreSQL'e bağlanmaya çalışır. Postgres kapalıysa WebApi başlatılamaz — önce `docker-compose up -d` çalıştırdığından emin ol.

> **RabbitMQ Uyarısı:** RabbitMQ kuyrukuna harici bir TTL (message-ttl) politikası atanmışsa mesajlar anında silinebilir. Kontrol etmek için:
> ```powershell
> docker exec baykar_rabbitmq rabbitmqctl list_policies
> ```
> Varsa temizle:
> ```powershell
> docker exec baykar_rabbitmq rabbitmqctl clear_policy ttl-policy
> ```

---

## Öne Çıkan Teknik Kararlar

| Karar | Neden? |
|---|---|
| **Batch Mesaj Mimarisi** | Race condition'ı ortadan kaldırmak için Frame+GroundTruth+Inference tek mesajda |
| **Explicit ACK Stratejisi** | DB'ye yazıldıktan SONRA ACK → veri kaybı imkansız |
| **NACK + Requeue** | DB çökerse mesajlar kuyrukta bekler, geri gelince devam eder |
| **Clean Architecture** | Domain katmanı altyapıya bağımlı değil |
| **Prefetch = 1** | Sıralı mesaj işleme → FK bütünlüğü korunur |
| **IoU Metric** | Bounding box doğruluğunu ölçen endüstri standardı |
