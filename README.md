# SwitchBot Home Control

Windows ネイティブアプリケーションで SwitchBot デバイスを簡単に制御できます。

## 特徴

- **超軽量**: メモリ使用量わずか 15-40 MB
- **高速起動**: 起動時間 0.5-2 秒
- **システムトレイ常駐**: バックグラウンドで快適に動作
- **ワンクリック操作**: トレイアイコンから直接電球を ON/OFF
- **Web UI**: ブラウザからも操作可能
- **トースト通知**: 操作結果を視覚的に確認
- **不快指数の Discord 通知**: 温湿度計の値から 10 分おきに不快指数を計算して通知

## システム要件

- Windows 10/11 (64-bit)
- .NET 8.0 Runtime

## インストール

### 1. .NET 8.0 Runtime をインストール

https://dotnet.microsoft.com/download/dotnet/8.0

### 2. SwitchBot API トークンの取得

1. SwitchBot アプリを開く
2. プロフィール > 設定 を選択
3. 「アプリバージョン」を 10 回タップして開発者オプションを有効化
4. 「トークン&シークレット」を取得

参考: https://support.switch-bot.com/hc/en-us/articles/12822710195351

### 3. 設定ファイルを作成

`.env.example` を `.env` にコピーして、SwitchBot API の認証情報を設定：

```bash
SWITCHBOT_TOKEN=your_token_here
SWITCHBOT_SECRET=your_secret_here
PORT=3000
DISCORD_WEBHOOK_URL=https://discord.com/api/webhooks/xxxxx/yyyyy
```

`DISCORD_WEBHOOK_URL` は不快指数の通知先です。空のままなら通知は無効になります（詳細は「[不快指数の Discord 通知](#不快指数の-discord-通知)」）。

### 4. 実行

```
bin\Release\net8.0-windows\win-x64\publish\SwitchBotHomeControl.exe
```

### 5. スタートアップへの登録（任意）

サインイン時に自動起動させる場合は、次のスクリプトを実行します。
ユーザーのスタートアップフォルダ（`shell:startup`）にショートカットを作成します。

```powershell
powershell -ExecutionPolicy Bypass -File scripts\register-startup.ps1
```

既定では、このリポジトリの `bin\Release\net8.0-windows\win-x64\publish\SwitchBotHomeControl.exe` を登録します。
解除する場合は `-Unregister` を付けて実行します。

起動中は exe がロックされるため、再ビルドの前にトレイメニューの「終了」でアプリを止めてください。

## 使い方

### タスクトレイ操作

- **左クリック**: メニューを表示
- **右クリック**: メニューを表示
- **ダブルクリック**: ブラウザでコントロール画面を開く

### メニュー構成

```
🏠 SwitchBot Home Control
━━━━━━━━━━━━━━━━━━━
📱 コントロール画面を開く
━━━━━━━━━━━━━━━━━━━
💡 [電球名] - ON
💡 [電球名] - OFF
💡 [電球名] - Toggle
━━━━━━━━━━━━━━━━━━━
終了
```

### Web UI

ブラウザで `http://localhost:3000` にアクセスすると、すべてのデバイスを制御できます。

- デバイス一覧の表示
- ON/OFF/Toggle 操作
- 明るさ調整（対応デバイス）
- トースト通知で操作確認

### 不快指数の Discord 通知

起動中は 10 分おきに、すべての温湿度計の気温と湿度から不快指数を計算します。
体感が「何も感じない」「快い」以外の温湿度計があれば、Discord へ 1 件の投稿で通知します。
起動直後にも 1 回計算します。

計算式と体感の区分は [不快指数を知って快適な空間を（エアコン総本舗）](https://ac.fj-tec.co.jp/%E3%81%8A%E5%BD%B9%E7%AB%8B%E3%81%A1%E6%83%85%E5%A0%B1/%E4%B8%8D%E5%BF%AB%E6%8C%87%E6%95%B0%E3%82%92%E7%9F%A5%E3%81%A3%E3%81%A6%E5%BF%AB%E9%81%A9%E3%81%AA%E7%A9%BA%E9%96%93%E3%82%92/) に従います。

```
DI = 0.81T + 0.01H × (0.99T − 14.3) + 46.3   （T: 気温 ℃、H: 湿度 %）
```

不快指数は小数第 1 位に四捨五入し、各区分は下限を含みます（例: 70.0 は「暑くない」）。

| 不快指数 | 体感 | 通知 |
|---------|------|------|
| 55 未満 | 寒い | する |
| 55 以上 60 未満 | 肌寒い | する |
| 60 以上 65 未満 | 何も感じない | しない |
| 65 以上 70 未満 | 快い | しない |
| 70 以上 75 未満 | 暑くない | する |
| 75 以上 80 未満 | やや暑い | する |
| 80 以上 85 未満 | 暑くて汗が出る | する |
| 85 以上 | 暑くてたまらない | する |

投稿には温湿度計の名前（SwitchBot アプリでの名前）を必ず含めます。

```
**不快指数のお知らせ**（2026/09/23 14:30）
- **温湿度計 デスク**: 不快指数 79.0「やや暑い」（気温 28.0℃ / 湿度 75%）
- **温湿度計 寝室**: 不快指数 52.2「寒い」（気温 10.0℃ / 湿度 50%）
```

- 対象は温湿度計（`Meter`・`MeterPlus`・`MeterPro`・`MeterPro(CO2)`・`WoIOSensor`）です。ハブ内蔵のセンサーは対象外です。
- 電池残量 0、または気温と湿度がともに 0 の温湿度計は、測定値なしとみなして通知から除きます。
- 不快な状態が続く間は、10 分ごとに通知します。

#### Webhook URL の設定

1. Discord で通知先チャンネルの「チャンネルの編集」>「連携サービス」>「ウェブフック」を開く
2. 「新しいウェブフック」を作成し、「ウェブフック URL をコピー」を押す
3. リポジトリ直下の `.env` に `DISCORD_WEBHOOK_URL=コピーした URL` を追記する
4. アプリを再起動する

`https://discord.com/api/webhooks/` で始まらない値を設定すると、起動時に警告を表示し、通知を無効にして起動します。
Webhook URL を知っていれば誰でも投稿できるため、`.env` を共有しないでください。

## 開発

### ビルド方法

```bash
dotnet build
```

または、`build.bat` をダブルクリック

### テスト

```bash
dotnet test
```

### リリースビルド

```bash
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true
```

実行ファイルは `bin\Release\net8.0-windows\win-x64\publish\` に生成されます。

## プロジェクト構成

```
switchbot-home-control/
├── Program.cs                          # メインエントリポイント + トレイアイコン
├── Api/
│   ├── SwitchBotClient.cs             # SwitchBot API クライアント
│   └── Models/
│       └── Device.cs                   # データモデル
├── Monitoring/
│   ├── DiscomfortIndex.cs              # 不快指数の計算と体感の区分
│   ├── MeterStatus.cs                  # 温湿度計の判定と測定値の読み取り
│   ├── DiscomfortAlert.cs              # 通知対象の抽出と投稿文の組み立て
│   └── DiscomfortMonitorService.cs     # 10 分おきの監視
├── Notifications/
│   └── DiscordWebhookClient.cs         # Discord Webhook への投稿
├── scripts/
│   └── register-startup.ps1            # スタートアップ登録
├── tests/
│   └── SwitchBotHomeControl.Tests/     # xUnit テスト
├── WebServer/
│   └── Controllers/
│       └── DevicesController.cs        # ASP.NET Core コントローラー
├── wwwroot/
│   └── index.html                      # Web UI
├── .env                                # 環境変数
├── .env.example                        # 環境変数サンプル
├── .gitignore                          # Git 除外設定
├── SwitchBotHomeControl.csproj         # プロジェクトファイル
├── build.bat                           # ビルドスクリプト
├── run.bat                             # 実行スクリプト
└── README.md                           # このファイル
```

## API エンドポイント

| エンドポイント | メソッド | 説明 |
|------------|--------|------|
| `/api/devices` | GET | デバイス一覧を取得 |
| `/api/devices/status` | GET | 全デバイスのステータス取得 |
| `/api/devices/:id/status` | GET | 特定デバイスのステータス取得 |
| `/api/devices/:id/on` | POST | デバイスをオン |
| `/api/devices/:id/off` | POST | デバイスをオフ |
| `/api/devices/:id/toggle` | POST | デバイスをトグル |
| `/api/devices/:id/brightness` | POST | 明るさを設定 |
| `/api/devices/:id/command` | POST | カスタムコマンド送信 |

## 技術スタック

- **.NET 8.0**: モダンな C# ランタイム
- **ASP.NET Core**: 軽量 Web サーバー
- **Windows Forms**: システムトレイアイコン
- **HMAC-SHA256**: SwitchBot API 認証

## パフォーマンス

| 項目 | 値 |
|-----|-----|
| メモリ使用量 | 15-40 MB |
| 実行ファイルサイズ | 390 KB |
| 起動時間 | 0.5-2 秒 |
| CPU 使用率（アイドル時） | 0-0.1% |

## 対応デバイス

SwitchBot Hub 経由で接続されているすべてのデバイスに対応：

- Bot
- Plug
- Curtain
- Color Bulb
- LED Strip Light
- 赤外線リモコンデバイス（エアコン、テレビなど）

## トラブルシューティング

### アプリが起動しない

1. .NET 8.0 Runtime がインストールされているか確認
2. `.env` ファイルが正しく配置されているか確認
3. SWITCHBOT_TOKEN と SWITCHBOT_SECRET が正しく設定されているか確認

### タスクトレイアイコンが表示されない

Windows の「隠れているインジケーターを表示する」をクリックして確認してください。

### 電球が読み込まれない

メニューを開いたときに自動的に読み込まれます。エラーメッセージが表示される場合は、API 認証情報を確認してください。

### デバイスが見つからない

- SwitchBot Hub が正しく設定されているか確認
- デバイスが Hub に接続されているか確認
- SwitchBot アプリでデバイスが正常に動作するか確認

### Discord に通知が届かない

- `.env` の `DISCORD_WEBHOOK_URL` が正しいか確認
- 温湿度計がすべて「何も感じない」「快い」の範囲なら通知しません
- 失敗の記録は Windows のイベントビューアー（「Windows ログ」>「Application」、ソース `.NET Runtime`）に残ります

### API 認証エラー

- `.env` ファイルのトークンとシークレットが正しいか確認
- トークンの有効期限を確認

## ライセンス

MIT License

## 参考リンク

- [SwitchBot API Documentation](https://github.com/OpenWonderLabs/SwitchBotAPI)
- [SwitchBot Support](https://support.switch-bot.com/)
- [.NET 8.0 Download](https://dotnet.microsoft.com/download/dotnet/8.0)
