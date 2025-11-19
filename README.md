# SwitchBot Home Control

Windows ネイティブアプリケーションで SwitchBot デバイスを簡単に制御できます。

## 特徴

- **超軽量**: メモリ使用量わずか 15-40 MB
- **高速起動**: 起動時間 0.5-2 秒
- **システムトレイ常駐**: バックグラウンドで快適に動作
- **ワンクリック操作**: トレイアイコンから直接電球を ON/OFF
- **Web UI**: ブラウザからも操作可能
- **トースト通知**: 操作結果を視覚的に確認

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
```

### 4. 実行

```
bin\Release\net8.0-windows\win-x64\publish\SwitchBotHomeControl.exe
```

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

## 開発

### ビルド方法

```bash
dotnet build
```

または、`build.bat` をダブルクリック

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

### API 認証エラー

- `.env` ファイルのトークンとシークレットが正しいか確認
- トークンの有効期限を確認

## ライセンス

MIT License

## 参考リンク

- [SwitchBot API Documentation](https://github.com/OpenWonderLabs/SwitchBotAPI)
- [SwitchBot Support](https://support.switch-bot.com/)
- [.NET 8.0 Download](https://dotnet.microsoft.com/download/dotnet/8.0)
