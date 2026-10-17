using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using UnityEngine;
using DBP.Missions;
namespace DBP.Ranking
{
    public sealed class LeaderboardRepository
    {
        [Serializable] sealed class Document
        {
            public int version;
            public List<RankedResult> combat;
            public List<RankedResult> knowledge;
        }
        [Serializable] sealed class Envelope { public string payload, checksum; }
        static string Checksum(string value)
        {
            using(var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(value))).Replace("-","");
        }
        static string Serialize(Document document)
        {
            string payload = JsonUtility.ToJson(document);
            return JsonUtility.ToJson(new Envelope { payload = payload, checksum = Checksum(payload) },true);
        }
        readonly string path;
        Document data;
        public bool RecoveredBackup { get; private set; }
        public List<RankingGroup> Groups => data.combat.Concat(data.knowledge).Select(r=>r.group).Distinct().OrderBy(g=>g.missionId,StringComparer.Ordinal).ThenBy(g=>g.difficulty).ThenBy(g=>g.contentVersion,StringComparer.Ordinal).ThenBy(g=>g.rulesVersion,StringComparer.Ordinal).ThenBy(g=>g.upgrades,StringComparer.Ordinal).Select(g=>g.Copy()).ToList();
        public LeaderboardRepository(string filePath)
        {
            path = filePath;
            if (!File.Exists(path) && !File.Exists(path+".bak")) { data = new Document { version = 1, combat = new List<RankedResult>(), knowledge = new List<RankedResult>() }; return; }
            try { data = Read(path); }
            catch (Exception e) when (e is IOException || e is InvalidDataException || e is ArgumentException)
            {
                try { data = Read(path+".bak"); RecoveredBackup = true; }
                catch (Exception backup) when (backup is IOException || backup is InvalidDataException || backup is ArgumentException)
                { throw new InvalidDataException("Không đọc được thành tích hoặc bản sao lưu. Dữ liệu gốc được giữ nguyên.", e); }
            }
        }
        static Document Read(string file)
        {
            var envelope = JsonUtility.FromJson<Envelope>(File.ReadAllText(file,Encoding.UTF8));
            if(envelope == null || string.IsNullOrWhiteSpace(envelope.payload) || envelope.checksum != Checksum(envelope.payload))
                throw new InvalidDataException("Dữ liệu bảng xếp hạng bị thiếu hoặc không toàn vẹn.");
            var doc = JsonUtility.FromJson<Document>(envelope.payload);
            if (doc == null || doc.version != 1 || doc.combat == null || doc.knowledge == null) throw new InvalidDataException("Định dạng bảng xếp hạng không hợp lệ.");
            foreach (var list in new[]{doc.combat,doc.knowledge})
            {
                var seen = new HashSet<string>(StringComparer.Ordinal);
                foreach (var r in list)
                {
                    if (r == null) throw new InvalidDataException("Thành tích rỗng.");
                    r.Validate();
                    if (!seen.Add(JsonUtility.ToJson(r.group)+"/"+r.profileId)) throw new InvalidDataException("Trùng thành tích của hồ sơ.");
                }
            }
            return doc;
        }
        public List<RankedRow> Rows(RankingGroup group, bool knowledge) => Leaderboard.Rank((knowledge ? data.knowledge : data.combat).Where(r=>r.group.Equals(group)),knowledge);
        public bool Record(RankedResult result)
        {
            if (result == null || result.outcome != MissionOutcome.Won || !result.eligible || string.IsNullOrWhiteSpace(result.profileId)) return false;
            result.Validate();
            var next = new Document { version = 1, combat = new List<RankedResult>(data.combat), knowledge = new List<RankedResult>(data.knowledge) };
            bool combat = Update(next.combat,result,Leaderboard.CombatCompare);
            bool knowledge = Update(next.knowledge,result,Leaderboard.KnowledgeCompare);
            if (!combat && !knowledge) return false;
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            File.WriteAllText(path+".tmp",Serialize(next),new UTF8Encoding(false));
            // If main was corrupt, never replace the good backup with that corrupt file.
            if (File.Exists(path)) File.Replace(path+".tmp",path,RecoveredBackup ? null : path+".bak");
            else File.Move(path+".tmp",path);
            data = next; RecoveredBackup = false;
            return true;
        }
        static bool Update(List<RankedResult> list, RankedResult result, Comparison<RankedResult> compare)
        {
            int index = list.FindIndex(r=>r.profileId==result.profileId && r.group.Equals(result.group));
            if (index >= 0 && (list[index].attemptId == result.attemptId || compare(result,list[index]) >= 0)) return false;
            if (index >= 0) list[index] = result.Copy(); else list.Add(result.Copy());
            return true;
        }
    }
}
