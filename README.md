# Manga-in-UA-Downloader

![GitHub Release Date - Published_At](https://img.shields.io/github/release-date/bigchunguspng/manga-in-ua-downloader)
![GitHub code size in bytes](https://img.shields.io/github/languages/code-size/bigchunguspng/manga-in-ua-downloader)
![GitHub commit activity (master)](https://img.shields.io/github/commit-activity/m/bigchunguspng/manga-in-ua-downloader)

CLI-тулза для завантаження манґи з сайту https://manga.in.ua.

## Основні можливості
- Завантаження ___одного___, ___усіх___ або ___декількох___ розділів манґи.
- Перегляд наявних на сайті розділів певної манґи.

## Встановлення

### Підготовка

Для роботи програми потрібен _.NET SDK 6.0_.
1. Для перевірки, чи встановлений він на вашій пекарні, пропишіть в терміналі `dotnet --list-sdks`.
2. Якщо нема, [завантажте](https://dotnet.microsoft.com/en-us/download/dotnet/6.0) і встановіть його.

### Встановлення
1. Завантажте [звідси](https://github.com/bigchunguspng/manga-in-ua-downloader/releases) **zip-архів** з останньою версією програми.
2. Розпакуйте.
3. Запустіть файл `install.bat` і дочекайтесь завершення його виконання.
4. Готово.

Також архів містить два файли для оновлення і видалення програми.

## Використання

Викликати програму можна через термінал або командний рядок, прописавши **MiUD** з будь-якого розташування.

### Приклади

```powershell
# завантаження одного розділу манґи
miud "https://manga.in.ua/chapters/….html"

# завантаження всіх розділів манґи
miud "https://manga.in.ua/mangas/….html"

# завантаження всіх розділів манґи з третього тому
miud "https://manga.in.ua/mangas/….html" -v 3

# завантаження всіх розділів манґи, починаючи з розділу 20
miud "https://manga.in.ua/mangas/….html" -fc 20

# перелік усіх розділів манґи
miud "https://manga.in.ua/mangas/….html" -lc
```

### Опції
| Опція                 | Або   | Аргумент | Опис                                                |
|:----------------------|:------|:---------|:----------------------------------------------------|
| `--chapter`           | `-c`  | Розділ № | Розділ, який слід завантажити.                      |
| `--from-chapter`      | `-fc` | Розділ № | Перший розділ, що слід завантажити.                 |
| `--to-chapter`        | `-tc` | Розділ № | Останній розділ, що слід завантажити.               |
| `--volume`            | `-v`  | Том №    | Том, розділи з якого слід завантажити.              |
| `--from-volume`       | `-fv` | Том №    | Перший том, що слід завантажити.                    |
| `--to-volume`         | `-tv` | Том №    | Останній том, що слід завантажити.                  |
| `--directory`         | `-d`  | \-       | Завантажує томи манґи до поточної директорії. [^1]  |
| `--chapterize`        | `-s`  | \-       | Зберігає вміст кожного розділу в окрему папку. [^2] |
| `--only-translator`   | `-o`  | Нік [^3] | Відфільтровує лише розділи з певним перекладом.     |
| `--prefer-translator` | `-p`  | Нік [^3] | Надає перевагу розділам з певним перекладом.        |
| `--list-chapters`     | `-lc` | \-       | Перелічує всі наявні на сайті розділи.              |
| `--list-selected`     | `-ls` | \-       | Перелічує всі розділи, що відповідають запиту.      |

Опції з аргументами передаються у форматі `опція аргумент` або `опція "аргумент з пробілами"`. Більшість з них мають сенс лише при завантаженні за посиланням на сторінку манґи. Останні дві дають змогу переглянути список розділів перед завантаженням. Якщо користувач не передасть жодних опцій, програма завантажить усі розділи відповідного тайтлу.

[^1]: За замовчуванням, у поточній директорії створюється папка з назвою манґи.
[^2]: За замовчуванням, сторінки розділів завантажуються прямо до тек відповідних томів.
[^3]: При введенні ніку регістр не має значення. Щоб програма розпізнала нік, достатньо передати хоча б його частину.
