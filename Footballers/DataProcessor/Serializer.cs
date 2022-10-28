namespace Footballers.DataProcessor
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Xml.Serialization;
    using Data;
    using Footballers.Data.Models.Enums;
    using Footballers.DataProcessor.ExportDto;
    using Newtonsoft.Json;
    using Formatting = Newtonsoft.Json.Formatting;

    public class Serializer
    {
        public static string ExportCoachesWithTheirFootballers(FootballersContext context)
        {
            var coaches = context.Coaches.
                           ToArray().
                           Where(c => c.Footballers.Count() >= 1).
                           OrderByDescending(c => c.Footballers.Count()).ThenBy(c => c.Name).
                           Select(c => new CoachExportDTO()
                           {
                               CoachName = c.Name,
                               FootballersCount = c.Footballers.Count().ToString(),
                               Footballers = c.Footballers.Select(f => new FootballerExportDTO()
                               {
                                   Name=f.Name,
                                   Position=Enum.GetName(typeof(PositionType),f.PositionType)
                               }).OrderBy(f=>f.Name).ToArray()
                           }).ToArray();

            StringBuilder sb = new StringBuilder();
            using StringWriter writer = new StringWriter(sb);
            XmlRootAttribute root = new XmlRootAttribute("Coaches");
            XmlSerializerNamespaces namespaces = new XmlSerializerNamespaces();
            namespaces.Add(string.Empty, string.Empty);
            XmlSerializer serializer = new XmlSerializer(typeof(CoachExportDTO[]), root);
            serializer.Serialize(writer, coaches,namespaces);

            return sb.ToString().Trim();
        }

        public static string ExportTeamsWithMostFootballers(FootballersContext context, DateTime date)
        {
            var teams = context.Teams.
                              ToArray().
                              Where(t => t.TeamsFootballers.Any(tf => tf.Footballer.ContractStartDate >= date)).
                              Select(t => new
                              {
                                  Name = t.Name,
                                  Footballers = t.TeamsFootballers.Where(f => f.Footballer.ContractStartDate >= date).
                                  OrderByDescending(f => f.Footballer.ContractEndDate).ThenBy(f => f.Footballer.Name).
                                  Select(f => new
                                  {
                                      FootballerName = f.Footballer.Name,
                                      ContractStartDate = f.Footballer.ContractStartDate.ToString("d", CultureInfo.InvariantCulture),
                                      ContractEndDate = f.Footballer.ContractEndDate.ToString("d", CultureInfo.InvariantCulture),
                                      BestSkillType = Enum.GetName(typeof(BestSkillType), f.Footballer.BestSkillType),
                                      PositionType = Enum.GetName(typeof(PositionType), f.Footballer.PositionType)
                                  }).ToArray()

                              }).OrderByDescending(t=>t.Footballers.Count()).ThenBy(t=>t.Name).ToArray().Take(5);

            return JsonConvert.SerializeObject(teams, Formatting.Indented);
        }
    }
}
