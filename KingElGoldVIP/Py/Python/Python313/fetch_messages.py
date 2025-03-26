from telethon import TelegramClient

# Replace with your own credentials
api_id = '23217044'  # You can get it from https://my.telegram.org/auth
api_hash = 'd1e520d5e1c34c41d5869834beeff40a'
phone_number = '00962795717083'

client = TelegramClient('session_name', api_id, api_hash)

async def main():
    await client.start(phone_number)
    
    # Get all dialogs (including groups)
    dialogs = await client.get_dialogs()

    for dialog in dialogs:
        if dialog.is_group:  # Check if the dialog is a group
            print(f"Fetching messages from group: {dialog.name} (ID: {dialog.id})")

            # Get all messages from the group (change the limit as needed)
            messages = await client.get_messages(dialog.id, limit=100)  # Adjust the limit as necessary

            for message in messages:
                print(f"Message from {message.sender_id}: {message.text}")

if __name__ == '__main__':
    import asyncio
    asyncio.run(main())

