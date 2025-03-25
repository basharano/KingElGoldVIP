import MetaTrader5 as mt5
import sys
import json

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
        order_type = position.type  # 0 = Buy, 1 = Sell
        lot_size = position.volume

        # Determine opposite order type for closing
        close_type = mt5.ORDER_SELL if order_type == mt5.ORDER_BUY else mt5.ORDER_BUY

        # Prepare the close request
        close_request = {
            "action": mt5.TRADE_ACTION_DEAL,
            "symbol": symbol,
            "volume": lot_size,
            "type": close_type,
            "position": ticket,
            "price": mt5.symbol_info_tick(symbol).bid if close_type == mt5.ORDER_SELL else mt5.symbol_info_tick(symbol).ask,
            "deviation": 10,
            "magic": 0,
            "comment": "Closing position",
            "type_filling": mt5.ORDER_FILLING_IOC
        }

        # Send the close order
        result = mt5.order_send(close_request)

        if result.retcode == mt5.TRADE_RETCODE_DONE:
            closed_positions.append({"ticket": ticket, "status": "closed"})
        else:
            closed_positions.append({"ticket": ticket, "status": "failed", "error": result.comment})

    # Shutdown MT5 connection
    mt5.shutdown()
    
    return json.dumps({"status": "success", "closed_positions": closed_positions})

if __name__ == "__main__":
    if len(sys.argv) > 1:
        symbol = sys.argv[1]
        print(close_positions_by_currency(symbol))
    else:
        print(json.dumps({"status": "error", "message": "No currency symbol provided"}))
