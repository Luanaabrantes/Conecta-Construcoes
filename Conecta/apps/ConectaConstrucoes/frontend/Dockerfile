# Stage 1: Build da aplicação React/Vite
FROM node:22-alpine AS build
WORKDIR /app

# Cache de dependências do package.json e package-lock.json
COPY package*.json ./
RUN npm ci

# Copia código-fonte e compila os arquivos estáticos
COPY . .
RUN npm run build

# Stage 2: Servidor Nginx otimizado para produção
FROM nginx:1.27-alpine AS final

# Remove a configuração padrão do Nginx
RUN rm -rf /etc/nginx/conf.d/default.conf

# Copia a configuração personalizada com suporte a SPA e proxy reverso
COPY nginx.conf /etc/nginx/conf.d/default.conf

# Copia os artefatos compilados da SPA
COPY --from=build /app/dist /usr/share/nginx/html

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
