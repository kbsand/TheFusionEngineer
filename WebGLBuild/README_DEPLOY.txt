TheFusionEngineer WebGL - GitHub Pages deployment

Upload the contents of this WebGLBuild folder so index.html is at the root
of the selected GitHub Pages source.

Local test:
  python -m http.server 8000 --directory WebGLBuild
  http://localhost:8000/

GitHub Pages:
  Repository Settings > Pages > Build and deployment
  Expected URL: https://<github-user>.github.io/<repository>/
