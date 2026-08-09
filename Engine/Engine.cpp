// Engine.cpp : Defines the functions for the static library.

#include "pch.h"
#include "framework.h"
#include <glad/glad.h>
#include <cstdio>

typedef void* (*GLADloadproc)(const char* name);

namespace {

GLuint g_VAO = 0;
GLuint g_VBO = 0;
GLuint g_ShaderProgram = 0;
GLint g_ViewProjLoc = -1;

const char* vertexShaderSrc = R"(
#version 460 core
layout (location = 0) in vec3 aPos;

uniform mat4 uViewProj;

void main()
{
    gl_Position = uViewProj * vec4(aPos, 1.0);
}
)";

const char* fragmentShaderSrc = R"(
#version 460 core
out vec4 FragColor;

void main()
{
    FragColor = vec4(1.0, 0.5, 0.2, 1.0);
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
        0.5f,  0.5f, 0.0f,  // 0: top right
        0.5f, -0.5f, 0.0f,  // 1: bottom right
        -0.5f,  0.5f, 0.0f,  // 2: top left
        -0.5f, -0.5f, 0.0f,  // 3: bottom left
    };

    unsigned int indices[] = {
        0, 1, 2,   // top right, bottom right, top left
        1, 3, 2    // bottom right, bottom left, top left
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
    
    glVertexAttribPointer(0, 3, GL_FLOAT, GL_FALSE, 3 * sizeof(float), (void*)0);
    glEnableVertexAttribArray(0);

    glBindBuffer(GL_ARRAY_BUFFER, 0);
    glBindVertexArray(0);

    g_ViewProjLoc = glGetUniformLocation(g_ShaderProgram, "uViewProj");
}

} // anonymous namespace

extern "C" {

void Engine_Init(void* getProcAddress)
{
    if (!gladLoadGLLoader((GLADloadproc)getProcAddress))
    {
        fprintf(stderr, "[Engine] Failed to initialize GLAD\n");
        return;
    }
    fprintf(stderr, "[Engine] GLAD initialized, GL version %s\n", glGetString(GL_VERSION));

    InitTriangle();
}

void Engine_RenderFrame(int fb, int width, int height, const float* viewProj)
{
    glBindFramebuffer(GL_FRAMEBUFFER, fb);
    glViewport(0, 0, width, height);

    glClearColor(0.1f, 0.1f, 0.1f, 1.0f);
    glClear(GL_COLOR_BUFFER_BIT);

    glUseProgram(g_ShaderProgram);

    float aspectRatio = (height > 0) ? (float)width / (float)height : 1.0f;
    glUniformMatrix4fv(g_ViewProjLoc, 1, GL_FALSE, viewProj);

    glBindVertexArray(g_VAO);
    glDrawElements(GL_TRIANGLES, 6, GL_UNSIGNED_INT, 0);
    glBindVertexArray(0);
}

}

// TODO: This is an example of a library function
void fnEngine()
{
    
}