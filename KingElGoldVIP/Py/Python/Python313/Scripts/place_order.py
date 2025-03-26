from MetaTrader5 import *
import time

# Connect to MT5
MT5Initialize()
MT5Login(242200436, password="New@1234", server="Exness-MT5Trial")

def place_order(symbol, volume, order_type):
    request = {
        "action": MT5_ORDER_SEND,
        "symbol": symbol,
        "volume": volume,
        "type": MT5_ORDER_BUY if order_type == "BUY" else MT5_ORDER_SELL,
        "price": MT5SymbolInfo(symbol).ask if order_type == "BUY" else MT5SymbolInfo(symbol).bid,
        "sl": 84000,   # Example Stop Loss
        "tp": 80000,   # Example Take Profit
        "deviation": 10,
        "magic": 123456,
        "comment": "Trade from C#",
        "type_filling": MT5_ORDER_FILLING_IOC,
        "type_time": MT5_ORDER_TIME_GTC,
    }

    result = MT5OrderSend(request)
    if result.retcode == 10009:
        print("Order placed successfully!")
    else:
        print(f"Order failed: {result.comment}")

    MT5Shutdown()

# Example usage
if __name__ == "__main__":
    place_order("BTCUSDm", 0.1, "SELL")
