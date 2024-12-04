INSERT INTO [Intersections] ([ProductID], [OptionID])
SELECT p.[ProductID], o.[OptionID]
FROM [Products] p
CROSS JOIN [Options] o;