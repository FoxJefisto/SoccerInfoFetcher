SELECT p.FirstName, p.LastName, c.ClubName, s.[Year],[Goals]
      ,[Assists]
      ,[Matches]
      ,[Minutes]
      ,[GoalPlusPass]
      ,[PenGoals]
      ,[DoubleGoals]
      ,[HatTricks]
      ,[AutoGoals]
      ,[YellowCards]
      ,[YellowRedCards]
      ,[RedCards]
      ,[FairPlayScore]
FROM PlayerStatistics ps
JOIN Seasons s ON ps.SeasonId = s.Id
JOIN Players p ON ps.PlayerId = p.Id
JOIN Clubs c ON ps.ClubId = c.Id
WHERE ClubName = 'Зенит' AND [Year] = '2021-2022'
ORDER BY GoalPlusPass DESC
