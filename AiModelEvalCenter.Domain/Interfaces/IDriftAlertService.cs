using AiModelEvalCenter.Domain.Entities;
using System;
using System.Threading.Tasks;

namespace AiModelEvalCenter.Domain.Interfaces
{
    public interface IDriftAlertService
    {
        /// <summary>
        /// Belirtilen model için son N inference'ın ortalama IoU'sunu hesaplar.
        /// Eşik altındaysa DriftAlert döndürür, değilse null.
        /// </summary>
        Task<DriftAlert?> CheckForDriftAsync(Guid modelId, int windowSize = 10);
    }
}
