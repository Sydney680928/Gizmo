using MOGWAI.Engine;
using MOGWAI.Objects;
using Gizmo.Builders;
using Gizmo;

namespace Gizmo.Primitives;

internal static class DialogPrimitives
{
    /// <summary>
    /// Pops a dialog definition record, shows a modal dialog via context.App.Run(),
    /// and pushes the result record.
    /// Must be called from the TG thread (inside an action block).
    /// </summary>
    public static async Task<EvalResult> Show(MogwaiEngine engine, UiContext context)
    {
        var sig = engine.StackSign(1);
        if (sig.Count == 0)
            return EvalResult.Failure(engine, Error.TooFewArgumentsError, "dialog.show");
        if (sig[0] != typeof(MOGRecord))
            return EvalResult.Failure(engine, Error.BadArgumentTypeError, "dialog.show");

        var dialogDef = engine.StackPopRecord();
        var result    = DialogBuilder.ShowModal(dialogDef, engine, context);
        engine.StackPush(result);

        return EvalResult.NoError;
    }
}
