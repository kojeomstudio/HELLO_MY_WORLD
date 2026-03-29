using System;
using System.Net.Sockets;

namespace GameServer.Utils
{
    /// <summary>
    /// 중앙화된 에러 핸들링 유틸리티
    /// </summary>
    public static class ErrorHandler
    {
        private static readonly Logger _logger = Logger.Instance;

        /// <summary>
        /// 예외를 안전하게 처리하고 로깅
        /// </summary>
        public static void Handle(Exception ex, string context, string additionalInfo = "")
        {
            var category = DetermineCategory(ex);
            var message = $"{context}: {ex.Message}";

            if (!string.IsNullOrEmpty(additionalInfo))
            {
                message += $" | {additionalInfo}";
            }

            if (IsCritical(ex))
            {
                _logger.Critical(category, message, ex);
            }
            else
            {
                _logger.Error(category, message, ex);
            }
        }

        /// <summary>
        /// 작업을 안전하게 실행하고 예외 처리
        /// </summary>
        public static bool TryExecute(Action action, string context, out Exception? exception)
        {
            exception = null;
            try
            {
                action();
                return true;
            }
            catch (Exception ex)
            {
                exception = ex;
                Handle(ex, context);
                return false;
            }
        }

        /// <summary>
        /// 값을 반환하는 작업을 안전하게 실행
        /// </summary>
        public static bool TryExecute<T>(Func<T> func, string context, out T? result, out Exception? exception)
        {
            result = default;
            exception = null;
            try
            {
                result = func();
                return true;
            }
            catch (Exception ex)
            {
                exception = ex;
                Handle(ex, context);
                return false;
            }
        }

        /// <summary>
        /// 네트워크 관련 예외인지 확인
        /// </summary>
        public static bool IsNetworkException(Exception ex)
        {
            return ex is SocketException ||
                   ex is System.IO.IOException ||
                   ex is ObjectDisposedException;
        }

        /// <summary>
        /// 데이터베이스 관련 예외인지 확인
        /// </summary>
        public static bool IsDatabaseException(Exception ex)
        {
            return ex.GetType().FullName?.Contains("Sqlite") == true ||
                   ex.GetType().FullName?.Contains("Sql") == true;
        }

        /// <summary>
        /// 치명적인 예외인지 확인
        /// </summary>
        private static bool IsCritical(Exception ex)
        {
            return ex is OutOfMemoryException ||
                   ex is StackOverflowException ||
                   ex is AccessViolationException ||
                   ex is AppDomainUnloadedException;
        }

        /// <summary>
        /// 예외 타입에 따라 카테고리 결정
        /// </summary>
        private static string DetermineCategory(Exception ex)
        {
            if (IsNetworkException(ex))
                return "Network";

            if (IsDatabaseException(ex))
                return "Database";

            if (ex is ArgumentException || ex is ArgumentNullException)
                return "Validation";

            if (ex is InvalidOperationException)
                return "Operation";

            if (ex is UnauthorizedAccessException)
                return "Security";

            return "General";
        }

        /// <summary>
        /// 사용자 친화적인 에러 메시지 생성
        /// </summary>
        public static string GetUserFriendlyMessage(Exception ex)
        {
            if (IsNetworkException(ex))
                return "Network connection error. Please check your connection and try again.";

            if (IsDatabaseException(ex))
                return "Database error occurred. Please contact server administrator.";

            if (ex is ArgumentException || ex is ArgumentNullException)
                return "Invalid input provided. Please check your request.";

            if (ex is UnauthorizedAccessException)
                return "Access denied. You don't have permission to perform this action.";

            return "An unexpected error occurred. Please try again later.";
        }
    }
}
