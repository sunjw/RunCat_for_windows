// Copyright 2025 Takuto Nakamura
//
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//
//        http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.

namespace RunCat365
{
    internal static class TreeFormatter
    {
        private static readonly bool isFullWidth = SupportedLanguageExtension.GetCurrentLanguage().IsFullWidth();

        private static string BranchMiddle => isFullWidth ? "├─" : "├─ ";
        private static string BranchLast => isFullWidth ? "└─" : "└─ ";
        private static string IndentContinue => isFullWidth ? "│　" : "│  ";
        private static string IndentLast => isFullWidth ? "　　" : "   ";

        internal static string CreateRoot(string content)
        {
            return content;
        }

        internal static string CreateNode(string content, bool isLast)
        {
            var prefix = isLast ? BranchLast : BranchMiddle;
            return $"{prefix}{content}";
        }

        internal static string CreateNestedNode(string content, bool parentIsLast, bool isLast)
        {
            var indent = parentIsLast ? IndentLast : IndentContinue;
            var prefix = isLast ? BranchLast : BranchMiddle;
            return $"{indent}{prefix}{content}";
        }
    }
}
