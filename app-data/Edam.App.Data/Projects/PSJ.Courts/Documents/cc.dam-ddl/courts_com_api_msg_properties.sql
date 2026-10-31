EXECUTE sp_addextendedproperty
   @name = 'Description', @value = 'Justice ',
   @level0type = N'SCHEMA',   @level0name = N'cc',
   @level1type = N'TABLE',    @level1name = N'Justice';

EXECUTE sp_addextendedproperty
   @name = 'Description', @value = 'Dbo',
   @level0type = N'SCHEMA',   @level0name = N'cc',
   @level1type = N'TABLE',    @level1name = N'Justice',
   @level2type = N'COLUMN',   @level2name = N'dbo';

