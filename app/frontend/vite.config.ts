import { defineConfig, loadEnv } from "vite";
import react from "@vitejs/plugin-react";

// https://vitejs.dev/config/
export default defineConfig(({ mode }) => {
    const env = loadEnv(mode, process.cwd(), '');
    const backendUrl = env.VITE_BACKEND_URI || 'http://localhost:57986';
    
    console.log(`Vite mode: ${mode}`)    
    console.log(`Frontend using backend URL: ${backendUrl}`);

    return {
        plugins: [react()],
        build: {
            outDir: "./build",
            emptyOutDir: true,
            sourcemap: true,
            rollupOptions: {
                output: {
                    manualChunks: id => {
                        if (id.includes("@fluentui/react-icons")) {
                            return "fluentui-icons";
                        } else if (id.includes("@fluentui/react")) {
                            return "fluentui-react";
                        } else if (id.includes("node_modules")) {
                            return "vendor";
                        }
                    }
                }
            },
            target: "esnext"
        },
        server: {
            proxy: {
                "/api/ask": {
                    target: backendUrl,
                    changeOrigin: true
                },
                "/api/chat": {
                    target: backendUrl,
                    changeOrigin: true
                },
                "/api/content": {
                    target: backendUrl,
                    changeOrigin: true
                },
                "/api/auth_setup": {
                     target: backendUrl,
                     changeOrigin: true
                            }
            }
        }
    };
});
