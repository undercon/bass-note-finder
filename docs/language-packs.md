# Create a language pack

Bass Note Finder reads JSON language packs from the `Languages` folder next to `BassNoteFinder.exe`. This folder includes a template with every translatable string and `nl.json`, a complete Dutch example.

## Create and install a pack

1. Copy `translation-template.json` to a new file in the same folder, such as `fr.json`.
2. Set `name` to the label that should appear in the **Language** picker. Use the language's own name, such as `Français`.
3. Translate the values under `strings`. Keep the keys exactly as written, including their capitalization.
4. Save the file as UTF-8, restart Bass Note Finder, and select your pack under **Language** in the footer.

A pack can contain just a few translations. For example, this file translates two labels and uses English for the rest.

```json
{
  "name": "Français",
  "strings": {
    "Language": "Langue",
    "Staff": "Portée"
  }
}
```

## Translate and test

- Preserve placeholders such as `{0}` and `{1}`. In `CorrectStudentNext`, `{0}` is the note the player played and `{1}` is the delay before the next note. You can reorder placeholders to fit the sentence.
- Omit entries or leave their values blank to use English. After selecting the pack, open Teacher and Student modes to check your labels, tooltips, and feedback after playing a note.
- Keep the filename stable. The app uses it to remember the selected pack between launches.
- If the pack is missing from the picker after restart, check that its extension is `.json`, its `name` is filled in, and its `strings` is a JSON object with quoted string values. Save the file in the `Languages` folder of the executable you are running.
