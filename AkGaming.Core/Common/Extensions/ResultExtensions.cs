using AkGaming.Core.Common.Generics;

namespace AkGaming.Core.Common.Extensions;

public static class ResultExtensions {
    // SYNC start → SYNC next
    public static Result Then(this Result result, Func<Result> next) {
        return result.IsSuccess ? next() : result;
    }

    // SYNC start → ASYNC next
    public static async Task<Result> Then(this Result result, Func<Task<Result>> next) {
        return result.IsSuccess ? await next() : result;
    }

    // ASYNC start → SYNC next
    public static async Task<Result> Then(this Task<Result> resultTask, Func<Result> next) {
        var result = await resultTask;
        return result.IsSuccess ? next() : result;
    }

    // ASYNC start → ASYNC next
    public static async Task<Result> Then(this Task<Result> resultTask, Func<Task<Result>> next) {
        var result = await resultTask;
        return result.IsSuccess ? await next() : result;
    }
}
