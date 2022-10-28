namespace Footballers.DataProcessor
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Xml.Serialization;
    using Data;
    using Footballers.Data.Models;
    using Footballers.Data.Models.Enums;
    using Footballers.DataProcessor.ImportDto;
    using Newtonsoft.Json;

    public class Deserializer
    {
        private const string ErrorMessage = "Invalid data!";

        private const string SuccessfullyImportedCoach
            = "Successfully imported coach - {0} with {1} footballers.";

        private const string SuccessfullyImportedTeam
            = "Successfully imported team - {0} with {1} footballers.";

        public static string ImportCoaches(FootballersContext context, string xmlString)
        {
            StringBuilder sb = new StringBuilder();
            XmlRootAttribute root = new XmlRootAttribute("Coaches");
            XmlSerializer serializer = new XmlSerializer(typeof(CoachImportDTO[]), root);
            using StringReader reader = new StringReader(xmlString);

            CoachImportDTO[] coaches = (CoachImportDTO[])serializer.Deserialize(reader);

            IList<Coach> validCoaches = new List<Coach>();

            foreach(var coach in coaches)
            {
                if(!IsValid(coach))
                {
                    sb.AppendLine(ErrorMessage);
                    continue;
                }

                Coach validCoach = new Coach()
                {
                    Name = coach.Name,
                    Nationality = coach.Nationality
                };

                foreach(var footballer in coach.Footballers)
                {
                    if(!IsValid(footballer))
                    {
                        sb.AppendLine(ErrorMessage);
                        continue;
                    }

                    bool isStartDateValid = DateTime.TryParseExact(footballer.ContractStartDate, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime contractStartDate);
                    bool isEndDateValid= DateTime.TryParseExact(footballer.ContractEndDate, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None,out DateTime contractEndDate);
                    bool isDateOrderCorrect = contractStartDate < contractEndDate;

                    if(isStartDateValid  && isEndDateValid  && isDateOrderCorrect)
                    {
                        validCoach.Footballers.Add(new Footballer()
                        {
                            Name = footballer.Name,
                            ContractStartDate = contractStartDate,
                            ContractEndDate = contractEndDate,
                            BestSkillType = (BestSkillType)footballer.BestSkillType,
                            PositionType = (PositionType)footballer.PositionType

                        });
                    }
                    else
                    {
                        sb.AppendLine(ErrorMessage);
                        continue;
                    }

             

                    


                }

                validCoaches.Add(validCoach);
                sb.AppendLine(String.Format(SuccessfullyImportedCoach, validCoach.Name, validCoach.Footballers.Count));
            }

            context.Coaches.AddRange(validCoaches);
            context.SaveChanges();

            return sb.ToString().TrimEnd();

        }
        
        public static string ImportTeams(FootballersContext context, string jsonString)
        {
            StringBuilder sb = new StringBuilder();
            var teams = JsonConvert.DeserializeObject<TeamImportDTO[]>(jsonString);

            IList<Team> validTeams=new List<Team>();    

            foreach(var team in teams)
            {
                if (!IsValid(team))
                {
                    sb.AppendLine(ErrorMessage);
                    continue;
                }

                if(String.IsNullOrEmpty(team.Name))
                {
                    sb.AppendLine(ErrorMessage);
                    continue;
                }

                if (String.IsNullOrEmpty(team.Nationality))
                {
                    sb.AppendLine(ErrorMessage);
                    continue;
                }


                int trophies = int.Parse(team.Trophies);
                if(trophies <=0)
                {
                    sb.AppendLine(ErrorMessage);
                    continue;
                }
              
                Team validTeam = new Team()
                {
                    Name=team.Name,
                    Nationality=team.Nationality,
                    Trophies=trophies
                };

                foreach(var footballer in team.Footballers.Distinct())
                {
                   if(!IsValid(footballer))
                    {
                        sb.AppendLine(ErrorMessage);
                        continue;
                    }

                    if (!context.Footballers.Any(f=>f.Id==footballer))
                    {
                        sb.AppendLine(ErrorMessage);
                        continue;
                    }

                    TeamFootballer teamFootballer = new TeamFootballer()
                    {
                        TeamId = team.Id,
                        FootballerId = footballer
                    };

                  

                    if(!validTeam.TeamsFootballers.Any(f=>f.FootballerId==teamFootballer.FootballerId))
                    {
                        validTeam.TeamsFootballers.Add(teamFootballer);
                    }

                }

                validTeams.Add(validTeam);
                sb.AppendLine(String.Format(SuccessfullyImportedTeam, validTeam.Name, validTeam.TeamsFootballers.Count));
            }

            context.Teams.AddRange(validTeams);
            context.SaveChanges();

            return sb.ToString().TrimEnd();
        }
        

        private static bool IsValid(object dto)
        {
            var validationContext = new ValidationContext(dto);
            var validationResult = new List<ValidationResult>();

            return Validator.TryValidateObject(dto, validationContext, validationResult, true);
        }
    }
}
