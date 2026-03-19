namespace ExperimentalControlPlatform.Runtime.StopConditions;

public interface IStopConditionEvaluator
{
    StopEvaluationResult Evaluate(RuntimeRunContext context);
}
