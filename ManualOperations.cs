﻿
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Client
{
    public class ManualOperations
    {
        // login username
        private string username;

        /*
         * Function: SendLogMessage()
         * Parameters: ClientWebSocket clientWebSocket: login client socket
         *             string username: username of the client
         *             string level: level of the message to be sent
         *             string message: message value to be sent
         * Description: This function sends any logs to the server in specified format.
         * Return values: bool: true if positive server response
         */
        public static async Task<bool> SendLogMessage(ClientWebSocket clientWebSocket, string username, string level = "", string message = "")
        {
            // required variables
            string format = File.ReadAllText("../../config.txt").Replace("TextFormat: ", "").ToLower();
            string data = "";
            string timestamp = DateTime.UtcNow.ToString();

            // making message based on the format
            switch (format)
            {
                case "json":
                    data = $"{{\"username\":\"{username}\",\"level\":\"{level}\",\"message\":\"{message}\",\"timestamp\":\"{timestamp}\"}}";
                    break;
                case "xml":
                    data = $"<username>{username}</username><level>{level}</level><message>{message}</message><timestamp>{timestamp}</timestamp>";
                    break;
                default:
                    data = $"{username}|{level}|{message}|{timestamp}";
                    break;
            }

            try
            {
                // Convert the JSON message to a byte array
                byte[] messageBytes = Encoding.UTF8.GetBytes(data);

                // Send the JSON message to the server
                await clientWebSocket.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, CancellationToken.None);

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending log message: {ex.Message}");
                return false;
            }

            string stringResponse = "";
            try
            {
                // getting the response from server
                var buffer = new byte[1024 * 4];
                WebSocketReceiveResult result;
                var response = new ArraySegment<byte>(buffer);
                result = await clientWebSocket.ReceiveAsync(response, CancellationToken.None);
                stringResponse = Encoding.UTF8.GetString(response.Array, 0, result.Count);

            }
            catch
            {
                stringResponse = "";
            }
            Console.WriteLine($"Response from server: {stringResponse}");
            if (stringResponse.Contains("Error"))
            {
                return false;
            }

            return true;
        }

        /*
         * Function: Login()
         * Parameters: Uri serverUri: server url
         *             bool manual: manual flag
         * Description: This function starts the login process for any user and provides response from server appropriately
         * Return values: clientWebSocket: login client socket value
         */
        public async Task<ClientWebSocket> Login(Uri serverUri, bool manual)
        {
            Console.WriteLine("------Login Test Started------");
            // creating a new client
            ClientWebSocket client = new ClientWebSocket();

            // connecting to server
            try
            {
                await client.ConnectAsync(serverUri, CancellationToken.None);

                // taking input of username

                if (manual)
                {
                    Console.Write("Enter your username: ");
                    username = Console.ReadLine();
                }

                // sending username to verify the user
                if (!await SendLogMessage(client, username, "REQ", "login"))
                {
                    Console.WriteLine("------Login Test Failed------\n");
                    return null;
                }

            }
            catch (Exception e)
            {
                Console.WriteLine($"Could not connect to server: {e.Message}");
                Console.WriteLine("------Login Test Failed------\n");
                return null;
            }
            Console.WriteLine("------Login Test Successful------\n");
            return client;
        }
    }
}
