using System;
using System.Collections.Generic;
using UnityEngine;

namespace DBP.Quiz
{
    /// Vỏ ngoài của file câu hỏi: { "schemaVersion": 1, "questions": [ ... ] }.
    /// Các trường thừa trong JSON (source, eventId, sceneTags, revision...) được JsonUtility bỏ qua.
    [Serializable]
    public class QuizQuestionFile
    {
        public int schemaVersion;
        public Question[] questions;
    }

    /// Bản đọc tối thiểu để chạy thử. Khi bộ đọc JSON của Long (T11) xong, đưa danh sách Question
    /// của Long vào QuestionSelector và có thể bỏ file này. Đọc file bằng UTF-8.
    public static class QuizQuestionParser
    {
        public static List<Question> Parse(string json)
        {
            var result = new List<Question>();
            if (string.IsNullOrWhiteSpace(json)) return result;

            QuizQuestionFile file = JsonUtility.FromJson<QuizQuestionFile>(json);
            if (file != null && file.questions != null) result.AddRange(file.questions);
            return result;
        }

        public static List<Question> ParseMany(IEnumerable<string> jsonFiles)
        {
            var all = new List<Question>();
            if (jsonFiles == null) return all;
            foreach (string json in jsonFiles) all.AddRange(Parse(json));
            return all;
        }
    }
}
