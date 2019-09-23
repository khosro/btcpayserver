dotnet build  .\BTCPayServer\BTCPayServer.csproj
start "btcpayserver-web-develop"  dotnet run -p  .\BTCPayServer\BTCPayServer.csproj  --conf="%Btcpay_Data_Home%\btcpay\btcpay.config"  --network=regtest  --chains "btc,eth" --btcexplorerurl http://127.0.0.1:24444 --btcexplorercookiefile "%Btcpay_Data_Home%\NBXplorer\data\RegTest\.cookie"


rem 