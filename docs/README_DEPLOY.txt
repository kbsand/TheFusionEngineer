TheFusionEngineer WebGL - GitHub Pages deployment

Upload target:
  Publish the contents of this WebGLBuild folder so index.html is at the
  root of the selected GitHub Pages source.

Expected URL:
  https://<github-user>.github.io/<repository>/

GitHub Pages setting:
  Repository Settings > Pages > Build and deployment
  Select the branch/folder or GitHub Actions workflow that contains these files.

Local test:
  python -m http.server 8000 --directory WebGLBuild
  Open http://localhost:8000/

Rebuild:
  Open the Unity 6000.5.2f1 Web Build Profile and build to WebGLBuild with
  Development Build disabled. Keep Gzip compression and Decompression
  Fallback enabled for static hosting such as GitHub Pages.
