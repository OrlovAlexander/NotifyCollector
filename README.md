# NotifyCollector
Принимает уведомления в виде текста. Уведомления аккумулируются в каналах. Канал определяется ключом в виде строки. Под капотом Dictionary<,>. Маршрутизации нет. Выполняет некоторую операцию (к примеру отправка письма) по критерию. Критерий - истечение некоторого заданного времени от времени последнего уведомления в канале.
