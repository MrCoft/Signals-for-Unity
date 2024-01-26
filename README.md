<br/>

<h1 align='center'>Signals for Unity</h1>

<p align='center'><b>Redirect Vite's development server root to a custom URL</b></p>

<p align='center'>Useful for projects with nested or multiple entry points.</p>

<br/>

## Installation

```shell
npm install -D @netglade/vite-plugin-root-redirect
```

## Example

How to launch Vite in development mode aimed at `src/views/Dashboard/index.html`:

Example folder structure:

```
├── src
│   ├── views
│   │   ├── Dashboard
│   │   │   ├── index.html
│   │   │   ├── index.ts
│   │   │   // other framework-specific files
```

Add plugin to your `vite.config.ts`:

```typescript
import { defineConfig } from 'vite'
import path from 'path'
// Import plugin
import { rootRedirect } from '@netglade/vite-plugin-root-redirect'

export default defineConfig({
  build: {
    rollupOptions: {
      input: {
        dashboard: path.resolve(__dirname, 'src/views/Dashboard/index.html'),
      }
    }
  },
  
  plugins: [
    // Use plugin
    rootRedirect({
      url: 'http://localhost:5173/src/views/Dashboard/index.html'
    }),
  ],
})
```
## rootRedirect API

**rootRedirect(options)**

Plugin options:

```typescript
{
  // `url` - the URL that Vite's root will redirect to
  url: string
}
```

## Motivation

This plugin is useful for projects with nested or multiple index.html entry points. It only helps during development while using the `vite` command.

[Vite supports multiple entry-points.](https://vitejs.dev/guide/build.html#multi-page-app) The starting URL can be somewhat modified, but it is defined in multiple places:

- You can [open the app automatically in the browser on launch.](https://vitejs.dev/config/server-options.html#server-open) This URL can be modified.

- When Vite starts, it prints a message into the terminal with the starting URL. This URL cannot be changed.

```shell
VITE v4.4.9  ready in 767 ms

➜  Local:   http://localhost:5173/
```

- You could have a back-end setup to proxy to the Vite server during development, where the starting URL might also have to be specified.
- The [Vite extension for VS Code](https://marketplace.visualstudio.com/items?itemName=antfu.vite) has its custom extension config for which URL it will start.

Some developers don't like to auto-open the browser when running Vite and want to open the app manually, this way their browser doesn't have to remember a URL specific to their project. This also helps when working with multiple projects.

## Possible improvements

- Only require relative path in URL and read the server host name and port dynamically from Vite config, e.g. `/src/views/Dashboard/index.html`. This would add support for when the port is already in use and the next one is taken instead.

## License

[MIT](LICENSE)

