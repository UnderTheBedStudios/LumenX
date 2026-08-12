// Engine.cpp : Defines the functions for the shared library.

#include "pch.h"
#include "framework.h"
#include <glad/glad.h>
#include <cstdio>
#include <chrono>
#include <math.h>
#include <unistd.h>
#include <string>
#include <glm.hpp>
#include <gtc/matrix_transform.hpp>
#include <gtc/type_ptr.hpp>

#define STB_IMAGE_IMPLEMENTATION
#include <stb_image.h>

typedef void* (*GLADloadproc)(const char* name);

namespace {

GLuint g_VAO = 0;
GLuint g_VBO = 0;
GLuint g_ShaderProgram = 0;
GLint g_ViewProjLoc = -1;
GLuint g_TextureID = 0;

glm::vec3 g_LightDir = glm::normalize(glm::vec3(0.3f, 1.0f, 0.2f));
glm::vec3 g_LightColor = glm::vec3(1.0f, 1.0f, 1.0f);
glm::vec3 g_ViewPos    = glm::vec3(0.0f);

GLint g_ModelLoc = -1, g_LightDirLoc = -1, g_LightColorLoc = -1, g_ViewPosLoc = -1;

std::string g_AssetRoot;

GLuint g_ShadowFBO = 0;
GLuint g_ShadowMap = 0;
const unsigned int SHADOW_WIDTH = 2048, SHADOW_HEIGHT = 2048;

GLuint g_DepthShaderProgram = 0;
GLint g_DepthLightSpaceLoc = -1, g_DepthModelLoc = -1;

GLint g_LightSpaceLoc = -1, g_ShadowMapLoc = -1;

glm::mat4 g_LightSpaceMatrix = glm::mat4(1.0f);

const char* vertexShaderSrc = R"(
#version 460 core
layout (location = 0) in vec3 aPos;
layout (location = 1) in vec2 aTexCoord;
layout (location = 2) in vec3 aNormal;

uniform mat4 uModel;
uniform mat4 uViewProj;
uniform mat4 uLightSpaceMatrix;

out vec2 vTexCoord;
out vec3 vNormal;
out vec3 vFragPos;
out vec4 vFragPosLightSpace;

void main()
{
    vec4 worldPos = uModel * vec4(aPos, 1.0);
    gl_Position = uViewProj * worldPos;

    vFragPos = worldPos.xyz;
    vNormal = mat3(transpose(inverse(uModel))) * aNormal;
    vTexCoord = aTexCoord;
    vFragPosLightSpace = uLightSpaceMatrix * worldPos;
}
)";

const char* fragmentShaderSrc = R"(
#version 460 core
out vec4 FragColor;

in vec2 vTexCoord;
in vec3 vNormal;
in vec3 vFragPos;

uniform sampler2D uTexture;
uniform sampler2D uShadowMap;
uniform vec4 vertexColor;
uniform vec4 ambientLightColor;

uniform vec3 uLightDir;      // normalized, pointing FROM the surface TOWARD the light
uniform vec3 uLightColor;
uniform vec3 uViewPos;       // camera world position, for specular

in vec4 vFragPosLightSpace;

float ShadowCalculation(vec4 fragPosLightSpace, vec3 normal, vec3 lightDir)
{
    vec3 projCoords = fragPosLightSpace.xyz / fragPosLightSpace.w;
    projCoords = projCoords * 0.5 + 0.5;

    if (projCoords.z > 1.0)
        return 0.0;

    float currentDepth = projCoords.z;
    float bias = max(0.005 * (1.0 - dot(normal, lightDir)), 0.0005);

    float shadow = 0.0;
    vec2 texelSize = 1.0 / textureSize(uShadowMap, 0);
    for (int x = -1; x <= 1; ++x)
        for (int y = -1; y <= 1; ++y)
        {
            float pcfDepth = texture(uShadowMap, projCoords.xy + vec2(x, y) * texelSize).r;
            shadow += currentDepth - bias > pcfDepth ? 1.0 : 0.0;
        }
    shadow /= 9.0;

    return shadow;
}

void main()
{
    vec3 norm = normalize(vNormal);

    float diff = max(dot(norm, uLightDir), 0.0);
    vec3 diffuse = diff * uLightColor;

    float shadow = ShadowCalculation(vFragPosLightSpace, norm, uLightDir);
    vec3 lighting = ambientLightColor.rgb + (1.0 - shadow) * diffuse;

    vec4 texColor = texture(uTexture, vTexCoord) * vertexColor;
    FragColor = vec4(texColor.rgb * lighting, texColor.a);
}
)";

const char* depthVertexShaderSrc = R"(
#version 460 core
layout (location = 0) in vec3 aPos;

uniform mat4 uLightSpaceMatrix;
uniform mat4 uModel;

void main()
{
    gl_Position = uLightSpaceMatrix * uModel * vec4(aPos, 1.0);
}
)";

const char* depthFragmentShaderSrc = R"(
#version 460 core
void main() { }
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
        // pos                  // uv           // normal
        // Front (z = 0)
        0.5f,  0.5f,  0.0f,    1.0f, 1.0f,      0.0f, 0.0f, 1.0f,
        0.5f, -0.5f,  0.0f,    1.0f, 0.0f,      0.0f, 0.0f, 1.0f,
        -0.5f,  0.5f,  0.0f,    0.0f, 1.0f,     0.0f, 0.0f, 1.0f,
        -0.5f, -0.5f,  0.0f,    0.0f, 0.0f,     0.0f, 0.0f, 1.0f,
        // Back (z = -1)
        -0.5f,  0.5f, -1.0f,    1.0f, 1.0f,     0.0f, 0.0f, -1.0f,
        -0.5f, -0.5f, -1.0f,    1.0f, 0.0f,     0.0f, 0.0f, -1.0f,
        0.5f,  0.5f, -1.0f,    0.0f, 1.0f,      0.0f, 0.0f, -1.0f,
        0.5f, -0.5f, -1.0f,    0.0f, 0.0f,      0.0f, 0.0f, -1.0f,
        // Left (x = -0.5)
        -0.5f,  0.5f,  0.0f,    1.0f, 1.0f,     -1.0f, 0.0f, 0.0f,
        -0.5f, -0.5f,  0.0f,    1.0f, 0.0f,     -1.0f, 0.0f, 0.0f,
        -0.5f,  0.5f, -1.0f,    0.0f, 1.0f,     -1.0f, 0.0f, 0.0f,
        -0.5f, -0.5f, -1.0f,    0.0f, 0.0f,     -1.0f, 0.0f, 0.0f,
        // Right (x = 0.5)
        0.5f,  0.5f, -1.0f,    1.0f, 1.0f,      1.0f, 0.0f, 0.0f,
        0.5f, -0.5f, -1.0f,    1.0f, 0.0f,      1.0f, 0.0f, 0.0f,
        0.5f,  0.5f,  0.0f,    0.0f, 1.0f,      1.0f, 0.0f, 0.0f,
        0.5f, -0.5f,  0.0f,    0.0f, 0.0f,      1.0f, 0.0f, 0.0f,
        // Top (y = 0.5)
        -0.5f,  0.5f, -1.0f,    0.0f, 1.0f,     0.0f, 1.0f, 0.0f,
        0.5f,  0.5f, -1.0f,    1.0f, 1.0f,      0.0f, 1.0f, 0.0f,
        -0.5f,  0.5f,  0.0f,    0.0f, 0.0f,     0.0f, 1.0f, 0.0f,
        0.5f,  0.5f,  0.0f,    1.0f, 0.0f,      0.0f, 1.0f, 0.0f,
        // Bottom (y = -0.5)
        -0.5f, -0.5f,  0.0f,    0.0f, 1.0f,     0.0f, -1.0f, 0.0f,
        0.5f, -0.5f,  0.0f,    1.0f, 1.0f,      0.0f, -1.0f, 0.0f,
        -0.5f, -0.5f, -1.0f,    0.0f, 0.0f,     0.0f, -1.0f, 0.0f,
        0.5f, -0.5f, -1.0f,    1.0f, 0.0f,      0.0f, -1.0f, 0.0f,
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
    
    // Pos
    glVertexAttribPointer(0, 3, GL_FLOAT, GL_FALSE, 8 * sizeof(float), (void*)0);
    glEnableVertexAttribArray(0);

    // UV
    glVertexAttribPointer(1, 2, GL_FLOAT, GL_FALSE, 8 * sizeof(float), (void*)(3 * sizeof(float)));
    glEnableVertexAttribArray(1);

    // Normals
    glVertexAttribPointer(2, 3, GL_FLOAT, GL_FALSE, 8 * sizeof(float), (void*)(5 * sizeof(float)));
    glEnableVertexAttribArray(2);

    glBindBuffer(GL_ARRAY_BUFFER, 0);
    glBindVertexArray(0);

    g_ViewProjLoc    = glGetUniformLocation(g_ShaderProgram, "uViewProj");
    g_ModelLoc       = glGetUniformLocation(g_ShaderProgram, "uModel");
    g_LightDirLoc    = glGetUniformLocation(g_ShaderProgram, "uLightDir");
    g_LightColorLoc  = glGetUniformLocation(g_ShaderProgram, "uLightColor");
    g_ViewPosLoc     = glGetUniformLocation(g_ShaderProgram, "uViewPos");
    g_LightSpaceLoc  = glGetUniformLocation(g_ShaderProgram, "uLightSpaceMatrix");
    g_ShadowMapLoc   = glGetUniformLocation(g_ShaderProgram, "uShadowMap");
}

void InitShadowMap()
{
    glGenFramebuffers(1, &g_ShadowFBO);

    glGenTextures(1, &g_ShadowMap);
    glBindTexture(GL_TEXTURE_2D, g_ShadowMap);
    glTexImage2D(GL_TEXTURE_2D, 0, GL_DEPTH_COMPONENT, SHADOW_WIDTH, SHADOW_HEIGHT, 0, GL_DEPTH_COMPONENT, GL_FLOAT, nullptr);
    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_NEAREST);
    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_NEAREST);
    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_CLAMP_TO_BORDER);
    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_CLAMP_TO_BORDER);
    float borderColor[] = { 1.0f, 1.0f, 1.0f, 1.0f };
    glTexParameterfv(GL_TEXTURE_2D, GL_TEXTURE_BORDER_COLOR, borderColor);

    glBindFramebuffer(GL_FRAMEBUFFER, g_ShadowFBO);
    glFramebufferTexture2D(GL_FRAMEBUFFER, GL_DEPTH_ATTACHMENT, GL_TEXTURE_2D, g_ShadowMap, 0);
    glDrawBuffer(GL_NONE);
    glReadBuffer(GL_NONE);

    if (glCheckFramebufferStatus(GL_FRAMEBUFFER) != GL_FRAMEBUFFER_COMPLETE)
        fprintf(stderr, "[Engine] Shadow FBO incomplete\n");

    glBindFramebuffer(GL_FRAMEBUFFER, 0);

    GLuint vs = CompileShader(GL_VERTEX_SHADER, depthVertexShaderSrc);
    GLuint fs = CompileShader(GL_FRAGMENT_SHADER, depthFragmentShaderSrc);
    g_DepthShaderProgram = glCreateProgram();
    glAttachShader(g_DepthShaderProgram, vs);
    glAttachShader(g_DepthShaderProgram, fs);
    glLinkProgram(g_DepthShaderProgram);
    glDeleteShader(vs);
    glDeleteShader(fs);

    g_DepthLightSpaceLoc = glGetUniformLocation(g_DepthShaderProgram, "uLightSpaceMatrix");
    g_DepthModelLoc      = glGetUniformLocation(g_DepthShaderProgram, "uModel");
}

void UpdateLightSpaceMatrix()
{
    float sceneRadius = 10.0f; // TODO: derive from real scene bounds once you have more objects

    glm::vec3 lightPos = g_LightDir * sceneRadius;
    glm::mat4 lightView = glm::lookAt(lightPos, glm::vec3(0.0f), glm::vec3(0.0f, 1.0f, 0.0f));
    glm::mat4 lightProj = glm::ortho(-sceneRadius, sceneRadius, -sceneRadius, sceneRadius, 0.1f, sceneRadius * 2.0f);

    g_LightSpaceMatrix = lightProj * lightView;
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
    InitShadowMap();
    UpdateLightSpaceMatrix();

    g_AssetRoot = assetRoot;
    std::string texturePath = g_AssetRoot + "/Engine/TestTextures/wall.jpg";
    LoadTexture(texturePath.c_str());
}

void Engine_RenderFrame(int fb, int width, int height, const float* viewProj, const float* model)
{
    // --- Pass 1: render depth from the light's POV ---
    glViewport(0, 0, SHADOW_WIDTH, SHADOW_HEIGHT);
    glBindFramebuffer(GL_FRAMEBUFFER, g_ShadowFBO);
    glClear(GL_DEPTH_BUFFER_BIT);

    glEnable(GL_CULL_FACE);
    glCullFace(GL_FRONT);

    glUseProgram(g_DepthShaderProgram);
    glUniformMatrix4fv(g_DepthLightSpaceLoc, 1, GL_FALSE, glm::value_ptr(g_LightSpaceMatrix));
    glUniformMatrix4fv(g_DepthModelLoc, 1, GL_FALSE, model);

    glBindVertexArray(g_VAO);
    glDrawElements(GL_TRIANGLES, 36, GL_UNSIGNED_INT, 0);
    glBindVertexArray(0);

    glDisable(GL_CULL_FACE);

    // --- Pass 2: normal scene render, sampling the shadow map ---
    glBindFramebuffer(GL_FRAMEBUFFER, fb);
    glViewport(0, 0, width, height);

    glClearColor(0.1f, 0.1f, 0.1f, 1.0f);
    glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);

    glUseProgram(g_ShaderProgram);

    glUniformMatrix4fv(g_ViewProjLoc, 1, GL_FALSE, viewProj);
    glUniformMatrix4fv(g_ModelLoc, 1, GL_FALSE, model);
    glUniformMatrix4fv(g_LightSpaceLoc, 1, GL_FALSE, glm::value_ptr(g_LightSpaceMatrix));
    glUniform3fv(g_LightDirLoc, 1, glm::value_ptr(g_LightDir));
    glUniform3fv(g_LightColorLoc, 1, glm::value_ptr(g_LightColor));
    glUniform3fv(g_ViewPosLoc, 1, glm::value_ptr(g_ViewPos));

    int vertexColorLocation = glGetUniformLocation(g_ShaderProgram, "vertexColor");
    int ambientLightColor = glGetUniformLocation(g_ShaderProgram, "ambientLightColor");
    glUniform4f(vertexColorLocation, 1.0f, 1.0f, 1.0f, 1.0f);
    glUniform4f(ambientLightColor, 0.15f, 0.15f, 0.15f, 1.0f);

    glActiveTexture(GL_TEXTURE0);
    glBindTexture(GL_TEXTURE_2D, g_TextureID);
    glUniform1i(glGetUniformLocation(g_ShaderProgram, "uTexture"), 0);

    glActiveTexture(GL_TEXTURE1);
    glBindTexture(GL_TEXTURE_2D, g_ShadowMap);
    glUniform1i(g_ShadowMapLoc, 1);

    glBindVertexArray(g_VAO);
    glPolygonMode(GL_FRONT_AND_BACK, GL_FILL);
    glDrawElements(GL_TRIANGLES, 36, GL_UNSIGNED_INT, 0);
    glBindVertexArray(0);
}

void Engine_SetLight(const float* lightDir, const float* lightColor, const float* viewPos)
{
    g_LightDir   = glm::normalize(glm::vec3(lightDir[0], lightDir[1], lightDir[2]));
    g_LightColor = glm::vec3(lightColor[0], lightColor[1], lightColor[2]);
    g_ViewPos    = glm::vec3(viewPos[0], viewPos[1], viewPos[2]);
    UpdateLightSpaceMatrix();
}

}

// TODO: This is an example of a library function
void fnEngine()
{
    
}