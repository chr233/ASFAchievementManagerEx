using ArchiSteamFarm.Localization;
using SteamKit2;

namespace ASFAchievementManagerEx.Core.Callbacks;

internal abstract class AbstrackMsgCallback<T> : CallbackMsg
{
    internal readonly T Response;
    internal readonly bool Success;

    internal AbstrackMsgCallback(JobID jobID, T msg, Func<T, EResult> eresultGetter, string error)
    {
        if (msg == null)
            throw new ArgumentNullException(nameof(msg));

        JobID = jobID ?? throw new ArgumentNullException(nameof(jobID));
        Success = eresultGetter(msg) == EResult.OK;
        Response = msg;

        if (!Success)
            ASFLogger.LogGenericError(string.Format(Strings.ErrorFailingRequest, error));
    }
}
