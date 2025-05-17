using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GroupProject.Services
{
    public class ChallengeService
    {
        private readonly string _challengeDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Challenges");
        private readonly string _statePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "State.xml");

        public void LoadChallenge(string filename)
        {
            var fullPath = Path.Combine(_challengeDir, filename);
            if (!File.Exists(fullPath))
                throw new FileNotFoundException("Challenge not found.", fullPath);

            File.Copy(fullPath, _statePath, overwrite: true);
        }

        public List<string> GetAvailableChallenges()
        {
            if (!Directory.Exists(_challengeDir))
                return new List<string>();

            return Directory.GetFiles(_challengeDir, "*.xml")
                            .Select(Path.GetFileName)
                            .ToList();
        }
    }
}
