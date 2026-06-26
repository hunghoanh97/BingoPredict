// Dựa trên file auto-gen bởi ML.NET Model Builder (đã đổi namespace cho tầng Infrastructure).
using Microsoft.ML;
using Microsoft.ML.Data;

namespace Bingo.Infrastructure.Ml;

public partial class PredictNumberModel
{
    public class ModelInput
    {
        [LoadColumn(0)]
        [ColumnName(@"Draw Time")]
        public string Draw_Time { get; set; } = string.Empty;

        [LoadColumn(1)]
        [ColumnName(@"Winning Result")]
        public float Winning_Result { get; set; }

        [LoadColumn(2)]
        [ColumnName(@"Sum")]
        public float Sum { get; set; }
    }

    public class ModelOutput
    {
        [ColumnName(@"Draw Time")]
        public float[] Draw_Time { get; set; } = Array.Empty<float>();

        [ColumnName(@"Winning Result")]
        public float Winning_Result { get; set; }

        [ColumnName(@"Sum")]
        public float Sum { get; set; }

        [ColumnName(@"Features")]
        public float[] Features { get; set; } = Array.Empty<float>();

        [ColumnName(@"Score")]
        public float Score { get; set; }
    }

    private static readonly string MLNetModelPath =
        Path.Combine(AppContext.BaseDirectory, "Models", "PredictNumberModel.mlnet");

    public static readonly Lazy<PredictionEngine<ModelInput, ModelOutput>> PredictEngine =
        new(CreatePredictEngine, isThreadSafe: true);

    private static PredictionEngine<ModelInput, ModelOutput> CreatePredictEngine()
    {
        var mlContext = new MLContext();
        ITransformer mlModel = mlContext.Model.Load(MLNetModelPath, out var _);
        return mlContext.Model.CreatePredictionEngine<ModelInput, ModelOutput>(mlModel);
    }

    public static ModelOutput Predict(ModelInput input) => PredictEngine.Value.Predict(input);
}
