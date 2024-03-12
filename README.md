
# Factoria - Репозиторий решений по фактографии, т.е. записи фактов

Под фактами мы понимаем документы в частности мультимедиа, в частности файлы, и произвольную информацию о документах и окружающем мире (персоны, оганизацонные системы, геосистемы и др.). Целью решений является создание и поддержание коллекций документов, хранилищ, архивов, баз данных, электронных архивов и музеев и т.д. Принципиальной особенностью подхода является распределенность базы данных и документов (базы фактов). Система может использоваться как автономно, в том числе без использования сервера, так и корпоративно с полным контролем размещенния данных на своих (или отечественных) ресурсах.

Особенностями подхода являются:
- Ориентация на  структуризацию Semantic Web, используемые стандарты WWWW-консорциума
- Базой хранения данных и фактов являются так называемые кассеты
- Документы (файлы) сохраняются (upload) в неизмененном состоянии и их копии могут быть выкачаны (download) пользователю
- Документы (файлы) являются неизменными в процессе жизни кассеты, если требуется замена на исправленный вариант, то правильнее будет уничтожить этот документ и ввести новый.

В рамках общего подхода, создан и создается набор программ, представляющих собой разные этапы обработки документов и данных. Это - программы ввода/редактирования кассет, программы визуализации и редактирования данных и др. Программы предназначены как для пользователей, так и для разработчиков. Программы оформлены как VisualStudio-проекты, а весь репозиторий как Visual Studio Solution. В основном (пока всегда) программный код выполнен в среде MS .NET Core на языке C#. Документация создана и создается в MarkDown формате. 