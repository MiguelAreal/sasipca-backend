using System.IO;
using System.Threading.Tasks;
using global::sasipca_API.Data;
using global::sasipca_API.Models;
using Microsoft.AspNetCore.Http;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace sasipca_API.Services
{

        /// <summary>
        /// Serviço responsável pelo processamento de imagens, incluindo redimensionamento e compressão.
        /// </summary>
        public class ImageProcessingService
        {
            private readonly AzureStorageService _storageService;


            /// <summary>
            /// Construtor que inicializa uma instância do serviço de armazenamento do Azure.
            /// </summary>
            /// <param name="storageService"></param>
            public ImageProcessingService(AzureStorageService storageService)
            {
                _storageService = storageService;
            }

            /// <summary>
            /// Processa uma imagem carregada pelo utilizador, aplicando redimensionamento e compressão.
            /// </summary>
            /// <param name="file">O ficheiro de imagem enviado pelo utilizador.</param>
            /// <param name="maxWidth">Largura máxima permitida para a imagem (por defeito, 800px).</param>
            /// <param name="maxHeight">Altura máxima permitida para a imagem (por defeito, 800px).</param>
            /// <param name="quality">Qualidade da imagem guardada (por defeito, 75).</param>
            /// <returns>Uma matriz de bytes representando a imagem processada.</returns>
            public async Task<byte[]> ProcessImageAsync(IFormFile file, int maxWidth = 800, int maxHeight = 800, int quality = 75)
            {
                using var image = await Image.LoadAsync(file.OpenReadStream());

                // Redimensiona a imagem mantendo a proporção dentro das dimensões especificadas.
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Mode = ResizeMode.Max,
                    Size = new Size(maxWidth, maxHeight)
                }));

                // Guarda a imagem em memória com a qualidade especificada.
                using var outputStream = new MemoryStream();
                await image.SaveAsync(outputStream, new JpegEncoder { Quality = quality });

                return outputStream.ToArray();
            }


            /// <summary>
            /// Método auxiliar para processar as imagens.
            /// </summary>
            /// <param name="imagensFicheiros">Imagens dadas pelo utilizador.</param>
            /// <returns>Lista de imagens processadas</returns>
            public async Task<List<Imagens>> ProcessarImagens(List<IFormFile> imagensFicheiros)
            {
                var imagens = new List<Imagens>();
                foreach (var imagem in imagensFicheiros.Take(4)) // Máximo de 4 imagens
                {
                    var optimizedImage = await ProcessImageAsync(imagem); // Otimiza imagem

                    using var stream = new MemoryStream(optimizedImage);
                    string imageUrl = await _storageService.UploadImageStreamAsync(stream, imagem.ContentType);

                    imagens.Add(new Imagens { Url = imageUrl });
                }
                return imagens;
            }
        }
}
