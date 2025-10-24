using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using sasipca_API.Services.Interfaces;
using System;
using System.IO;
using System.Threading.Tasks;

namespace sasipca_API.Services
{
    public class FileStorageService : IFileStorageService
    {
        private readonly IWebHostEnvironment _env;
        private const string RootFolderName = "Storage"; // Pasta raiz para todos os uploads

        public FileStorageService(IWebHostEnvironment env)
        {
            _env = env;
        }

        /// <summary>
        /// Constrói o caminho completo para o diretório de destino.
        /// </summary>
        private string BuildDirectoryPath(string subdirectoryName)
        {
            // Combina o caminho raiz do conteúdo com a pasta "Storage" e o subdiretório (e.g., CampaignImages)
            return Path.Combine(_env.ContentRootPath, RootFolderName, subdirectoryName);
        }

        /// <summary>
        /// Constrói o caminho completo para o ficheiro.
        /// </summary>
        public string GetFilePath(string fileName, string subdirectoryName)
        {
            var directoryPath = BuildDirectoryPath(subdirectoryName);
            return Path.Combine(directoryPath, fileName);
        }

        /// <summary>
        /// Guarda um ficheiro no caminho especificado.
        /// </summary>
        public async Task<string> SaveFileAsync(IFormFile file, string subdirectoryName, string baseFileName)
        {
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("O ficheiro é nulo ou vazio.");
            }

            var directoryPath = BuildDirectoryPath(subdirectoryName);

            // 1. Garantir que o diretório existe
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            // 2. Construir o caminho completo do ficheiro
            var filePath = Path.Combine(directoryPath, baseFileName);

            // 3. Guardar o ficheiro assincronamente
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Retornamos o nome único do ficheiro (baseFileName) que será armazenado na BD
            return baseFileName;
        }

        /// <summary>
        /// Remove um ficheiro do disco.
        /// </summary>
        public void DeleteFile(string fileName, string subdirectoryName)
        {
            if (string.IsNullOrEmpty(fileName)) return;

            var filePath = GetFilePath(fileName, subdirectoryName);

            if (File.Exists(filePath))
            {
                try
                {
                    File.Delete(filePath);
                }
                catch (IOException)
                {
                    // Lógica para lidar com ficheiros bloqueados (opcional, mas útil)
                    // Poderíamos logar o erro ou tentar novamente. Por enquanto, apenas ignora.
                }
            }
        }
    }
}