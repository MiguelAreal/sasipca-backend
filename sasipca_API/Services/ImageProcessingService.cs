using System.IO;
using System.Threading.Tasks;
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


            public ImageProcessingService()
            {
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

        }
}
