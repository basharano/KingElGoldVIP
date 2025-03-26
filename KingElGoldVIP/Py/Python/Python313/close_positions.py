import MetaTrader5 as mt5
import sys
import json

# Define order types manually (in case they are missing)
ORDER_BUY = 0
ORDER_SELL = 1

def close_positions_by_currency(symbol):
    # Initialize MT5 connection
    if not mt5.initialize():
        return json.dumps({"status": "error", "message": "MT5 initialization failed"})

    # Get all open positions for the given currency
    positions = mt5.positions_get(symbol=symbol)
    
    if positions is None or len(positions) == 0:
        mt5.shutdown()
        return json.dumps({"status": "success", "message": f"No open positions found for {symbol}"})

    closed_positions = []

    for position in positions:
        ticket = position.ticket
        order_type = int(position.type)  # Ensure it's an integer
        lot_size = position.volume

        # Correctly determine the opposite order type
        if order_type == ORDER_BUY:
            close_type = ORDER_SELL
            price = mt5.symbol_info_tick(symbol).bid  # Sell at bid price
        else:  # ORDER_SELL
            close_type = ORDER_BUY
            price = mt5.symbol_info_tick(symbol).ask  # Buy at ask price

        # Prepare the close request
        close_request = {
            "action": mt5.TRADE_ACTION_DEAL,
            "symbol": symbol,
            "volume": lot_size,
            "type": close_type,
            "position": ticket,
            "price": price,
            "deviation": 10,
            "magic": 0,
            "comment": "Closing position",
            "type_filling": mt5.ORDER_FILLING_IOC
        }

        # Send the close order
        result = mt5.order_send(close_request)

        if result.retcode == mt5.TRADE_RETCODE_DONE:
            closed_positions.append({
                "ticket": ticket,
                "status": "closed"
            })
        else:
            closed_positions.append({
                "ticket": ticket,
                "status": "failed",
                "error": result.comment
            })

    # Shutdown MT5 connection
    mt5.shutdown()
    
    return json.dumps({"status": "success", "closed_positions": closed_positions}, indent=4)

# If the script is run from the command line, accept arguments
if __name__ == "__main__":
    if len(sys.argv) > 1:
        symbol = sys.argv[1]
        print(close_positions_by_currency(symbol))
    else:
        print(json.dumps({"status": "error", "message": "No currency symbol provided"}))