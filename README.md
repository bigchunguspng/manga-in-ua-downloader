# Manga-in-UA-Downloader

![GitHub Downloads (all assets, all releases)](https://img.shields.io/github/downloads/bigchunguspng/manga-in-ua-downloader/total?color=green)
![GitHub Downloads (all assets, latest release)](https://img.shields.io/github/downloads/bigchunguspng/manga-in-ua-downloader/latest/total?label=downloads%20(latest)&color=yellow)
![GitHub Release Date - Published_At](https://img.shields.io/github/release-date/bigchunguspng/manga-in-ua-downloader?color=yellow)
![GitHub commit activity (master)](https://img.shields.io/github/commit-activity/m/bigchunguspng/manga-in-ua-downloader)

CLI-тулза для завантаження манґи з сайту https://manga.in.ua.

## Основні можливості

- 🔍 Пошук манґи.
- 👀 Перегляд наявних на сайті розділів певної манґи.
- 💾 Завантаження ___одного___, ___усіх___ або ___декількох___ розділів манґи.

## Встановлення

### Підготовка

Для роботи програми потрібен _.NET SDK 6.0_.
1. Для перевірки, чи встановлений він на вашій пекарні, пропишіть в терміналі `dotnet --list-sdks`.
2. Якщо нема, [завантажте](https://dotnet.microsoft.com/en-us/download/dotnet/6.0) і встановіть його. Мешканці Linux 🐧 можуть зробити це через термінал:

```bash
sudo apt-get update && sudo apt-get install -y dotnet-sdk-6.0
```

### Встановлення
1. Завантажте [звідси](https://github.com/bigchunguspng/manga-in-ua-downloader/releases) **zip-архів** з останньою версією програми.
2. Розпакуйте.
3. Запустіть файл `install.bat` / `install.sh` і дочекайтесь завершення його виконання.
4. Готово.

Також архів містить два файли для оновлення і видалення програми.

## Використання

Викликати програму можна через термінал або командний рядок, прописавши **MiUD** з будь-якого розташування.

### Приклади

```powershell
# пошук манґи
miud -s azumanga
miud -s "chainsaw man"

# перелік усіх розділів манґи
miud "https://manga.in.ua/mangas/….html" -lc

# завантаження одного розділу манґи
miud "https://manga.in.ua/chapters/….html"

# завантаження всіх розділів манґи
miud "https://manga.in.ua/mangas/….html"

# завантаження деяких розділів манґи
miud "https://manga.in.ua/mangas/….html" -v 3 # лише з третього тому
miud "https://manga.in.ua/mangas/….html" -tv 3 # лише перші три томи
miud "https://manga.in.ua/mangas/….html" -fc 5 # починаючи з розділу №5
miud "https://manga.in.ua/mangas/….html" -fc 5 -tc 20 # з п'ятого по двадцятий
```
Приклад використання програми через [_Windows Terminal_](https://github.com/microsoft/terminal):

![miud windows terminal example (one piece ver.)](https://github.com/bigchunguspng/manga-in-ua-downloader/assets/70759405/3014e829-327d-4371-a548-8f6a699ce281)
![miud windows terminal example (dr. stone ver.)](https://github.com/bigchunguspng/manga-in-ua-downloader/assets/70759405/93e90e21-6b02-4492-aed8-367559ddb268)



### Опції
| Опція                 | Або   | Аргумент | Опис                                                 |
|:----------------------|:------|:---------|:-----------------------------------------------------|
| `--chapter`           | `-c`  | Розділ № | Розділ, що слід завантажити.                         |
| `--from-chapter`      | `-fc` | Розділ № | Перший розділ, що слід завантажити.                  |
| `--to-chapter`        | `-tc` | Розділ № | Останній розділ, що слід завантажити.                |
| `--volume`            | `-v`  | Том №    | Том, розділи якого слід завантажити.                 |
| `--from-volume`       | `-fv` | Том №    | Перший том, що слід завантажити.                     |
| `--to-volume`         | `-tv` | Том №    | Останній том, що слід завантажити.                   |
| `--directory`         | `-d`  | \-       | Завантажує томи манґи до поточної директорії. [^1]   |
| `--chapterize`        | `-cp` | \-       | Зберігає вміст кожного розділу до окремої теки. [^2] |
| `--cbz`               | `-z`  | \-       | Зберігає манґу у форматі ".cbz".                     |
| `--only-translator`   | `-o`  | Нік [^3] | Обирає лише розділи з певним перекладом.             |
| `--prefer-translator` | `-p`  | Нік [^3] | Надає перевагу розділам з певним перекладом.         |
| `--list-chapters`     | `-lc` | \-       | Перелічує всі розділи, що є на сайті.                |
| `--list-selected`     | `-ls` | \-       | Перелічує всі розділи, що відповідають запиту.       |
| `--search`            | `-s`  | Запит    | Здійснює пошук манґи.                                |

Опції з аргументами передаються у форматі `Опція Аргумент` або `Опція "Аргумент з пробілами"`. Більшість з них можна комбінувати у довільному порядку. Також, більшість з них має сенс лише при завантаженні _за посиланням на сторінку манґи_. Опції `--list-…` дають змогу переглянути список розділів перед завантаженням. Якщо користувач не передасть жодних опцій, програма завантажить УСІ розділи відповідного тайтлу.

За допомогою опції пошуку можна отримати посилання на потрібний тайтл для подальшого завантаження. Програма автоматично скопіює його у буфер обміну, або запропонує вибрати один зі знайдених тайтлів, якщо їх буде більше одного. Навігація між варіантами здійснюється клавішами `↑`, `↓` та `Enter`. 

[^1]: За замовчуванням, у поточній директорії створюється папка з назвою манґи.
[^2]: За замовчуванням, сторінки розділів завантажуються прямо до тек відповідних томів.
[^3]: При введенні ніку регістр не має значення. Щоб програма розпізнала нік, достатньо передати хоча б його частину.
