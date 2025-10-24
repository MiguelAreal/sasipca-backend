using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace sasipca_API.Services.Interfaces
{
    public interface IFileStorageService
    {
        /// <summary>
        /// Guarda um ficheiro no caminho especificado e retorna o nome único do ficheiro.
        /// </summary>
        /// <param name="file">O ficheiro IFormFile.</param>
        /// <param name="subdirectoryName">O subdiretório de destino (e.g., "CampaignImages").</param>
        /// <param name="baseFileName">O nome base único do ficheiro (sem o caminho completo).</param>
        /// <returns>O nome base único do ficheiro que foi guardado (e.g., "b23e-imagem.jpg").</returns>
        Task<string> SaveFileAsync(IFormFile file, string subdirectoryName, string baseFileName);

        /// <summary>
        /// Remove um ficheiro do disco.
        /// </summary>
        /// <param name="fileName">O nome base único do ficheiro a ser removido.</param>
        /// <param name="subdirectoryName">O subdiretório onde o ficheiro reside.</param>
        void DeleteFile(string fileName, string subdirectoryName);

        /// <summary>
        /// Obtém o caminho completo do ficheiro (para uso interno ou para devolver ao cliente, se necessário).
        /// </summary>
        /// <param name="fileName">O nome base único do ficheiro.</param>
        /// <param name="subdirectoryName">O subdiretório onde o ficheiro reside.</param>
        /// <returns>O caminho completo do ficheiro no sistema.</returns>
        string GetFilePath(string fileName, string subdirectoryName);
    }
}