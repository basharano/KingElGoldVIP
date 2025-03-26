import MetaTrader5 as mt5
import sys
import time

# Initialize MetaTrader 5 connection
if not mt5.initialize():
    print("MetaTrader 5 initialization failed. Error:", mt5.last_error())
    mt5.shutdown()
    sys.exit(1)

# Get parameters from command line
symbol = sys.argv[1]
lot_size = float(sys.argv[2])
stop_loss = float(sys.argv[3])
take_profit = float(sys.argv[4])
magic_number = int(sys.argv[5])
order_type_str = sys.argv[6].upper()

# Select the symbol
if not mt5.symbol_select(symbol, True):
    print(f"Failed to select symbol {symbol}")
    mt5.shutdown()
    sys.exit(1)

# Get current price
price = mt5.symbol_info_tick(symbol).ask if order_type_str == "BUY" else mt5.symbol_info_tick(symbol).bid
slippage = 5

# Determine order type
if order_type_str == "BUY":
    order_type = mt5.ORDER_TYPE_BUY
    if stop_loss >= price or take_profit <= price:
        print("Error: SL must be below price, and TP must be above price for BUY orders.")
        sys.exit(1)
elif order_type_str == "SELL":
    order_type = mt5.ORDER_TYPE_SELL
    if stop_loss <= price or take_profit >= price:
        print("Error: SL must be above price, and TP must be below price for SELL orders.")
        sys.exit(1)
else:
    print(f"Invalid order type: {order_type_str}. Use 'BUY' or 'SELL'.")
    sys.exit(1)

# Ensure SL/TP are correctly rounded
digits = mt5.symbol_info(symbol).digits
stop_loss = round(stop_loss, digits)
take_profit = round(take_profit, digits)

# Place order
order_request = {
    "action": mt5.TRADE_ACTION_DEAL,
    "symbol": symbol,
    "volume": lot_size,
    "type": order_type,
    "price": price,
    "slippage": slippage,
    "deviation": 20,
    "type_filling": mt5.ORDER_FILLING_IOC,
    "type_time": mt5.ORDER_TIME_GTC,
}

result = mt5.order_send(order_request)

if result.retcode != mt5.TRADE_RETCODE_DONE:
    print(f"Order failed, error code: {result.retcode}")
    print("Error description:", mt5.last_error())
    mt5.shutdown()
    sys.exit(1)

print(f"Order placed successfully, order ticket: {result.order}")

# Wait for position to appear
time.sleep(1)  # Small delay to allow the trade to be processed

# Get the position ID
position_id = None
positions = mt5.positions_get(symbol=symbol)
if positions:
    for pos in positions:
        if pos.ticket == result.order:  # Find matching position
            position_id = pos.ticket
            break

if not position_id:
    print("Error: Position not found. Try increasing the wait time.")
    mt5.shutdown()
    sys.exit(1)

# Modify SL/TP after order execution using the position ticket
modify_request = {
    "action": mt5.TRADE_ACTION_SLTP,
    "position": position_id,  # Use Position ID instead of Order ID
    "symbol": symbol,
    "sl": stop_loss,  # Set Stop Loss
    "tp": take_profit,  # Set Take Profit
}

modify_result = mt5.order_send(modify_request)

if modify_result.retcode != mt5.TRADE_RETCODE_DONE:
    print(f"Failed to modify SL/TP. Error code: {modify_result.retcode}")
    print("Error description:", mt5.last_error())
else:
    print(f"SL and TP updated successfully for position {position_id}")

# Shutdown MT5 connection
mt5.shutdown()
