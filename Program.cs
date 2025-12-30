using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using System;
using System.IO;
using System.Threading.Tasks;

class Program
{
    private const string ConnectionString = "Endpoint=sb://localhost;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;";

    static async Task Main(string[] args)
    {
        Console.Write("Nom du topic cible : ");
        string? topicName = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(topicName))
        {
            Console.WriteLine("Le nom du topic est requis.");
            return;
        }

        // Chemin du dossier "Carlo"
        string folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Carlo");

        if (!Directory.Exists(folderPath))
        {
            Console.WriteLine($"Le dossier '{folderPath}' est introuvable.");
            return;
        }

        // Parcourir tous les fichiers dans le dossier
        string[] files = Directory.GetFiles(folderPath);
        if (files.Length == 0)
        {
            Console.WriteLine($"Aucun fichier trouvé dans le dossier '{folderPath}'.");
            return;
        }

        foreach (string filePath in files)
        {
            string fileName = Path.GetFileName(filePath); // Nom du fichier
            string fileContent = await ReadFileContentAsync(filePath); // Contenu du fichier

            if (!string.IsNullOrWhiteSpace(fileContent))
            {
                await SendMessageAsync(topicName, fileName, fileContent);
            }
        }
    }

    private static async Task<string> ReadFileContentAsync(string filePath)
    {
        try
        {
            return await File.ReadAllTextAsync(filePath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur lors de la lecture du fichier '{filePath}' : {ex.Message}");
            return string.Empty;
        }
    }

    private static async Task SendMessageAsync(string topicName, string title, string content)
    {
        await using var client = new ServiceBusClient(ConnectionString);
        ServiceBusSender sender = client.CreateSender(topicName);

        // Ton shipment brut
        var shipmentPayload = System.Text.Json.JsonSerializer.Deserialize<object>(content);

        var envelope = new
        {
            EventId = Guid.NewGuid().ToString(),
            EventType = title.Replace(".json", ""),  // ou une valeur fixe
            CompanyCode = "ALJIB",
            ShipmentMainRef = "ALJIB/22/25081744",

            Shipment = shipmentPayload
        };

        string body = System.Text.Json.JsonSerializer.Serialize(envelope);

        var message = new ServiceBusMessage(body)
        {
            ContentType = "application/json",
            Subject = title
        };

        await sender.SendMessageAsync(message);

        Console.WriteLine($"Message envoyé : {title}");
    }
}