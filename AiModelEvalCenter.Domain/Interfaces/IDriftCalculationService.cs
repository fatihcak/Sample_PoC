using AiModelEvalCenter.Domain.Entities;
using System.Threading.Tasks;

namespace AiModelEvalCenter.Domain.Interfaces
{
    public interface IDriftCalculationService
    {
        DriftMetric CalculateDrift(ModelInference inference, GroundTruth truth);
    }
}
