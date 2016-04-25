using System;
using System.Collections.Generic;
using System.Linq;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("TimeVote", "ThermalCube", 0.1)]
    [Description("A plugin to show the current time and start votes on a daytime change.")]
    class TimeVote : RustPlugin
    {
        
        protected override void LoadDefaultConfig()
        {
            PrintWarning("[TimeVote] Creating config file");
            Config.Clear();
            Config["ChatPrefix"] = "[TimeVote]";
            Config["MinVotes"] = 3;
            Config["Percentage"] = 60.0f;
            SaveConfig();

        }


        bool isVoteRunning = false;
        Dictionary<ulong, bool> votes = new Dictionary<ulong, bool>();
        
        
        [ChatCommand("time")]
        void TimeCommand(BasePlayer ply, string cmd, string[] args)
        {
            Puts("TimeVote init");
            string prefix = (string)Config["ChatPrefix"];
            var sky = TOD_Sky.Instance;
            if(args.Length == 0)
            {
                string curTime = DateTime.MinValue.AddHours(Convert.ToDouble(sky.Cycle.Hour)).ToString("h:mm");
                rust.SendChatMessage(ply, prefix, curTime);
                return;
            }
            
            if(args[0].ToLower() == "vote")
            {
                if(ply.IsAdmin() || ply.IsDeveloper())
                {

                    if (isVoteRunning)
                    {
                        rust.SendChatMessage(ply, prefix, "There is already a vote running!");
                    }
                    else
                    {
                        isVoteRunning = true;
                        votes.Clear();
                        timer.Once(60.0f, () => {

                            if (votes.Count <= (int)Config["MinVotes"])
                            {
                                rust.BroadcastChat(prefix, "Not enough votes!");
                                Puts("Not enough votes!");
                                return;
                            }

                            int yes = votes.Where(v => v.Value == true).Count();
                            float perc = (float)(yes / votes.Count()) * 100;

                            Puts("The vote percentage was {0}", perc.ToString());

                            if (perc >= (float)Config["Percentage"])
                            {
                                rust.BroadcastChat(prefix, "The vote was successfull. Skipping to next morning...");
                                Puts("Vote succeeded");
                                sky.Cycle.Hour = 6.0f;
                            }
                            else
                            {
                                rust.BroadcastChat(prefix, "The vote was unsuccessfull. Good luck surviving the night...");
                                Puts("Vote failed");
                            }


                        });

                    }

                }
                else
                {
                    rust.SendChatMessage(ply, prefix, "You don't have the permissions to start a vote!");
                }
            }
            else
            {

                if (!isVoteRunning)
                {
                    rust.SendChatMessage(ply, prefix, "The is current vote!");
                    return;
                }

                switch (args[0].ToLower())
                {
                    case "day":
                        votes.Add(ply.userID, true);
                        rust.SendChatMessage(ply, prefix, "You voted for the sun!");
                        break;
                    case "night":
                        votes.Add(ply.userID, false);
                        rust.SendChatMessage(ply, prefix, "You voted for the moon!");
                        break;
                    default:
                        rust.SendChatMessage(ply, prefix, "please use 'day' or 'night' to vote!");
                        break;
                }

            }
        }



    }
}