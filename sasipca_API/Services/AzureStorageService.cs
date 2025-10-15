using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using sasipca_API.Models;
using sasipca_API.Services;

namespace sasipca_API.Services
{
    /// <summary>
    /// Serviço responsável pelo armazenamento de imagens no Azure Blob Storage.
    /// </summary>
    public class AzureStorageService
    {
        private readonly string _connectionString;
        private readonly string _containerName = "imagens-neighbourlink"; // Nome do container no Azure

        /// <summary>
        /// Construtor que inicializa o serviço com a connection string do Azure Blob Storage.
        /// </summary>
        /// <param name="configuration">Objeto de configuração da aplicação.</param>
        /// <exception cref="Exception">Lança uma exceção caso a connection string do Azure Blob Storage esteja ausente.</exception>
        public AzureStorageService(IConfiguration configuration)
        {
            _connectionString = configuration.GetValue<string>("AZURE_STORAGE_KEY") // Obtém a chave de conexão do Azure Blob Storage da variável de ambiente
                ?? throw new Exception("Azure Blob Storage connection string missing.");
        }


        /// <summary>
        /// Faz o upload de uma imagem para o Azure Blob Storage e retorna o URL da imagem.
        /// </summary>
        /// <param name="imageStream">Stream contendo a imagem a ser enviada.</param>
        /// <param name="contentType">Tipo de conteúdo da imagem (ex: image/jpeg, image/png).</param>
        /// <returns>Uma string contendo o URL público da imagem armazenada.</returns>
        public async Task<string> UploadImageStreamAsync(Stream imageStream, string contentType)
        {
            var blobServiceClient = new BlobServiceClient(_connectionString);
            var blobContainerClient = blobServiceClient.GetBlobContainerClient(_containerName);

            // Cria o container caso não exista, permitindo acesso público aos blobs.
            await blobContainerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

            // Gera um nome único para a imagem
            string fileName = $"{Guid.NewGuid()}.jpg";
            var blobClient = blobContainerClient.GetBlobClient(fileName);

            var blobHttpHeaders = new BlobHttpHeaders { ContentType = contentType };

            // Faz o upload do ficheiro para o blob
            await blobClient.UploadAsync(imageStream, blobHttpHeaders);

            // Retorna o URL da imagem armazenada
            return blobClient.Uri.ToString();
        }
    }
}
