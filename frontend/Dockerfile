# Use a lightweight Node.js image
FROM node:23-alpine
WORKDIR /app

# Copy package files and install dependencies
COPY package*.json ./
RUN npm install

# Copy the rest of your frontend code
COPY . .

# Expose the default Vite port
EXPOSE 5173

# Start the Vite server. 
# The "--host" flag is REQUIRED in Docker so it listens on 0.0.0.0 instead of just the container's internal localhost.
CMD ["npm", "run", "dev", "--", "--host"]