// Engine.cpp : Defines the functions for the static library.

#include "pch.h"
#include "framework.h"
#include <glad/glad.h>
#include <cstdio>
#include <chrono>
#include <math.h>
#include <unistd.h>
#include <string>

#define STB_IMAGE_IMPLEMENTATION
#include "vendor/stb/stb_image.h"

typedef void* (*GLADloadproc)(const char* name);

namespace {

GLuint g_VAO = 0;
GLuint g_VBO = 0;
GLuint g_ShaderProgram = 0;
GLint g_ViewProjLoc = -1;
GLuint g_TextureID = 0;

std::string g_AssetRoot;

auto start_time = std::chrono::steady_clock::now();

const char* vertexShaderSrc = R"(
#version 460 core
layout (location = 0) in vec3 aPos;
layout (location = 1) in vec2 aTexCoord;

uniform mat4 uViewProj;

out vec2 vTexCoord;

void main()
{
    gl_Position = uViewProj * vec4(aPos, 1.0);
    vTexCoord = aTexCoord;
}
)";

const char* fragmentShaderSrc = R"(
#version 460 core
out vec4 FragColor;

in vec2 vTexCoord;
uniform sampler2D uTexture;

uniform vec4 vertexColor;

void main()
{
    FragColor = texture(uTexture, vTexCoord);
}
)";

GLuint CompileShader(GLenum type, const char* source)
{
    GLuint shader = glCreateShader(type);
    glShaderSource(shader, 1, &source, nullptr);
    glCompileShader(shader);

    GLint success;
    glGetShaderiv(shader, GL_COMPILE_STATUS, &success);
    if (!success)
    {
        char infoLog[512];
        glGetShaderInfoLog(shader, sizeof(infoLog), nullptr, infoLog);
        fprintf(stderr, "[Engine] Shader compile error: %s\n", infoLog);
    }
    return shader;
}

void LoadTexture(const char* path)
{
    glGenTextures(1, &g_TextureID);
    glBindTexture(GL_TEXTURE_2D, g_TextureID);

    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_REPEAT);
    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_REPEAT);
    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR_MIPMAP_LINEAR);
    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR);

    stbi_set_flip_vertically_on_load(true);
    glPixelStorei(GL_UNPACK_ALIGNMENT, 1);

    int width, height, channels;

    char cwd[1024];
    getcwd(cwd, sizeof(cwd));
    fprintf(stderr, "[Engine] CWD: %s\n", cwd);

    unsigned char* data = stbi_load(path, &width, &height, &channels, 0);
    if (data)
    {
        GLenum format = (channels == 4) ? GL_RGBA : GL_RGB;
        glTexImage2D(GL_TEXTURE_2D, 0, format, width, height, 0, format, GL_UNSIGNED_BYTE, data);
        glGenerateMipmap(GL_TEXTURE_2D);
    }
    else
        fprintf(stderr, "[Engine] Failed to load texture: %s\n", path);
    stbi_image_free(data);
}

void InitTriangle()
{
    GLuint vertexShader = CompileShader(GL_VERTEX_SHADER, vertexShaderSrc);
    GLuint fragmentShader = CompileShader(GL_FRAGMENT_SHADER, fragmentShaderSrc);

    g_ShaderProgram = glCreateProgram();
    glAttachShader(g_ShaderProgram, vertexShader);
    glAttachShader(g_ShaderProgram, fragmentShader);
    glLinkProgram(g_ShaderProgram);

    GLint linkSuccess;
    glGetProgramiv(g_ShaderProgram, GL_LINK_STATUS, &linkSuccess);
    if (!linkSuccess)
    {
        char infoLog[512];
        glGetProgramInfoLog(g_ShaderProgram, sizeof(infoLog), nullptr, infoLog);
        fprintf(stderr, "[Engine] Shader link error: %s\n", infoLog);
    }

    // Shader objects are only needed during linking — safe to delete once linked
    glDeleteShader(vertexShader);
    glDeleteShader(fragmentShader);

    float vertices[] = {
        // pos                  // uv
        // Front (z = 0)
        0.5f,  0.5f,  0.0f,    1.0f, 1.0f,
        0.5f, -0.5f,  0.0f,    1.0f, 0.0f,
        -0.5f,  0.5f,  0.0f,    0.0f, 1.0f,
        -0.5f, -0.5f,  0.0f,    0.0f, 0.0f,
        // Back (z = -1)
        -0.5f,  0.5f, -1.0f,    1.0f, 1.0f,
        -0.5f, -0.5f, -1.0f,    1.0f, 0.0f,
        0.5f,  0.5f, -1.0f,    0.0f, 1.0f,
        0.5f, -0.5f, -1.0f,    0.0f, 0.0f,
        // Left (x = -0.5)
        -0.5f,  0.5f,  0.0f,    1.0f, 1.0f,
        -0.5f, -0.5f,  0.0f,    1.0f, 0.0f,
        -0.5f,  0.5f, -1.0f,    0.0f, 1.0f,
        -0.5f, -0.5f, -1.0f,    0.0f, 0.0f,
        // Right (x = 0.5)
        0.5f,  0.5f, -1.0f,    1.0f, 1.0f,
        0.5f, -0.5f, -1.0f,    1.0f, 0.0f,
        0.5f,  0.5f,  0.0f,    0.0f, 1.0f,
        0.5f, -0.5f,  0.0f,    0.0f, 0.0f,
        // Top (y = 0.5)
        -0.5f,  0.5f, -1.0f,    0.0f, 1.0f,
        0.5f,  0.5f, -1.0f,    1.0f, 1.0f,
        -0.5f,  0.5f,  0.0f,    0.0f, 0.0f,
        0.5f,  0.5f,  0.0f,    1.0f, 0.0f,
        // Bottom (y = -0.5)
        -0.5f, -0.5f,  0.0f,    0.0f, 1.0f,
        0.5f, -0.5f,  0.0f,    1.0f, 1.0f,
        -0.5f, -0.5f, -1.0f,    0.0f, 0.0f,
        0.5f, -0.5f, -1.0f,    1.0f, 0.0f,
    };

    unsigned int indices[] = {
        0, 1, 2,        1, 3, 2,      // front
        4, 5, 6,        5, 7, 6,      // back
        8, 9, 10,       9, 11, 10,    // left
        12, 13, 14,     13, 15, 14,  // right
        16, 17, 18,     17, 19, 18,  // top
        20, 21, 22,     21, 23, 22,  // bottom
    };

    unsigned int EBO;
    glGenBuffers(1, &EBO);

    glGenVertexArrays(1, &g_VAO);
    glGenBuffers(1, &g_VBO);


    glBindVertexArray(g_VAO);
    
    glBindBuffer(GL_ARRAY_BUFFER, g_VBO);
    glBufferData(GL_ARRAY_BUFFER, sizeof(vertices), vertices, GL_STATIC_DRAW);
    
    glBindBuffer(GL_ELEMENT_ARRAY_BUFFER, EBO);
    glBufferData(GL_ELEMENT_ARRAY_BUFFER, sizeof(indices), indices, GL_STATIC_DRAW);
    
    glVertexAttribPointer(0, 3, GL_FLOAT, GL_FALSE, 5 * sizeof(float), (void*)0);
    glEnableVertexAttribArray(0);

    glVertexAttribPointer(1, 2, GL_FLOAT, GL_FALSE, 5 * sizeof(float), (void*)(3 * sizeof(float)));
    glEnableVertexAttribArray(1);

    glBindBuffer(GL_ARRAY_BUFFER, 0);
    glBindVertexArray(0);

    g_ViewProjLoc = glGetUniformLocation(g_ShaderProgram, "uViewProj");
}

} // anonymous namespace

extern "C" {

void Engine_Init(void* getProcAddress, const char* assetRoot)
{
    if (!gladLoadGLLoader((GLADloadproc)getProcAddress))
    {
        fprintf(stderr, "[Engine] Failed to initialize GLAD\n");
        return;
    }
    fprintf(stderr, "[Engine] GLAD initialized, GL version %s\n", glGetString(GL_VERSION));

    glEnable(GL_DEPTH_TEST);

    InitTriangle();

    g_AssetRoot = assetRoot;
    std::string texturePath = g_AssetRoot + "/Engine/TestTextures/wall.jpg";
    LoadTexture(texturePath.c_str());
}

void Engine_RenderFrame(int fb, int width, int height, const float* viewProj)
{
    auto now = std::chrono::steady_clock::now();
    std::chrono::duration<double> elapsed = now - start_time;

    glBindFramebuffer(GL_FRAMEBUFFER, fb);
    glViewport(0, 0, width, height);

    glClearColor(0.1f, 0.1f, 0.1f, 1.0f);
    glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);

    glUseProgram(g_ShaderProgram);

    float timeValue = elapsed.count();
    float greenValue = (sin(timeValue) / 2.0f) + 0.5f;
    int vertexColorLocation = glGetUniformLocation(g_ShaderProgram, "vertexColor");
    glUniform4f(vertexColorLocation, 0.0f, greenValue, 0.0f, 1.0f);

    float aspectRatio = (height > 0) ? (float)width / (float)height : 1.0f;
    glUniformMatrix4fv(g_ViewProjLoc, 1, GL_FALSE, viewProj);

    glBindVertexArray(g_VAO);
    glPolygonMode(GL_FRONT_AND_BACK, GL_FILL);
    glDrawElements(GL_TRIANGLES, 36, GL_UNSIGNED_INT, 0);
    glBindVertexArray(0);
}

}

// TODO: This is an example of a library function
void fnEngine()
{
    
}