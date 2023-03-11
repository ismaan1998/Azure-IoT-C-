// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// This application uses the Azure IoT Hub device SDK for .NET
// For samples see: https://github.com/Azure/azure-iot-sdk-csharp/tree/master/iothub/device/samples

using System;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices;

namespace simulatedDevice
{
    class SimulatedDevice
    {
        private static DeviceClient s_deviceClient;

        // The device connection string to authenticate the device with your IoT hub.
        private const string s_connectionString = "connection string";
        private const string s_serviceConnectionString = "service connection string";

        // Async method to send simulated telemetry
        private static async void SendDeviceToCloudMessagesAsync()
        {
            // Initial telemetry values
            double minTemperature = 20;
            double minHumidity = 60;
            Random rand = new Random();

            while (true)
            {
                double currentTemperature = minTemperature + rand.NextDouble() * 15;
                double currentHumidity = minHumidity + rand.NextDouble() * 20;

                // Create JSON message
                var telemetryDataPoint = new
                {
                    temperature = currentTemperature,
                    humidity = currentHumidity
                };
                var messageString = JsonConvert.SerializeObject(telemetryDataPoint);
                var message = new Microsoft.Azure.Devices.Client.Message(Encoding.ASCII.GetBytes(messageString));

                // Add a custom application property to the message.
                // An IoT hub can filter on these properties without access to the message body.
                message.Properties.Add("temperatureAlert", (currentTemperature > 30) ? "true" : "false");

                // Send the tlemetry message
                await s_deviceClient.SendEventAsync(message).ConfigureAwait(false);
                Console.WriteLine("{0} > Sending message: {1}", DateTime.Now, messageString);

                await Task.Delay(1000).ConfigureAwait(false);
            }
        }


        private static async void ReceiveC2dAsync()
        {
            while (true)
            {
                Microsoft.Azure.Devices.Client.Message receivedMessage = await s_deviceClient.ReceiveAsync();
                if (receivedMessage == null) continue;
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Received message: {0}", Encoding.ASCII.GetString(receivedMessage.GetBytes()));
                Console.ResetColor();

                await s_deviceClient.CompleteAsync(receivedMessage);
            }
        }

        private async static Task SendCloudToDeviceMessageAsync(){
            ServiceClient serviceClient = ServiceClient.CreateFromConnectionString(s_serviceConnectionString);
            string targetDevice = "iot-dev1";
            var commandMessage = new Microsoft.Azure.Devices.Message(Encoding.ASCII.GetBytes("This is my c2d message"));
            await serviceClient.SendAsync(targetDevice, commandMessage);
        }



        // handle desired properties as well as update the reported properties. 
        static async void HandleDesiredPropertiesChange()
        {
            await s_deviceClient.SetDesiredPropertyUpdateCallbackAsync(async (desired,ctx) => 
            {
                Newtonsoft.Json.Linq.JValue fpsJson = desired["FPS"];
                var fps = fpsJson.Value;

                Console.WriteLine("Received desired FPS: {0}", fps);

                var reportedProperties = new Microsoft.Azure.Devices.Shared.TwinCollection();
                var properties = new Microsoft.Azure.Devices.Shared.TwinCollection();
                properties["FPS"] = fps;
           
               
                reportedProperties["weather"] = properties;

                await s_deviceClient.UpdateReportedPropertiesAsync(reportedProperties);

            },null);
        }
        private static void Main()
        {
            Console.WriteLine("IoT Hub Quickstarts - Simulated device. Ctrl-C to exit.\n");

            // Connect to the IoT hub using the MQTT protocol
            s_deviceClient = DeviceClient.CreateFromConnectionString(s_connectionString, Microsoft.Azure.Devices.Client.TransportType.Mqtt);
            // SendDeviceToCloudMessagesAsync();
            // ReceiveC2dAsync();
            HandleDesiredPropertiesChange();
            // SendCloudToDeviceMessageAsync();
            Console.ReadLine();
        }
    }
}
