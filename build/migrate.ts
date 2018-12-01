// yarn run ts-node ./scripts/migrate.ts

import * as fs from 'fs'
import { promisify } from 'util'
import * as path from 'path'

const dry = true;

(async () => {
    const postDir = path.resolve(__dirname, "../source/_posts")
    const posts = await promisify(fs.readdir)(postDir)
    let warn = 0

    for (const postName of posts) {
        const post = path.resolve(postDir, postName)
        const regex = new RegExp(String.raw`\ndate: (\d{4}-\d{2}-\d{2}) (.*)\n`)
        if (path.extname(post) === ".md") {
            const buffer = await promisify(fs.readFile)(post)
            const content = buffer.toString()
            const m = content.match(regex)
            const date = m[1];
            if (!date) {
                console.warn(`No date in ${post}`)
                warn += 1
            }
            const endIndex = m.index + m[0].length;

            const permalink = postName.split('.')[0]
            const year = date.substr(0, 4)
            const newPath = path.resolve(postDir, path.join(postDir, year, postName))
            const newContent = content.substr(0, endIndex) + `permalink: ${permalink}\n` + content.substr(endIndex);

            if (dry) {
                console.log({
                    year,
                    date,
                    newPath,
                    content: newContent.substr(0, endIndex + permalink.length + 15),
                })
            } else {
                try {
                    await promisify(fs.mkdir)(path.join(postDir, year))
                } catch {
                    // OK
                }

                await promisify(fs.rename)(post, newPath)
                await promisify(fs.writeFile)(newPath, newContent)
            }
        }
    }

    console.warn({ warn })
})()
